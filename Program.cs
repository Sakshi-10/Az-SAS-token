using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System;

class Program
{
    static void Main(string[] args)
    {
        
        string accountName = "<your-storage-account-name>";
        string accountKey = "<your-storage-account-key>";
        string containerName = "<your-container-name>"; // Specify the container name
        string blobName = "<your-blob-name>"; // Specify the blob name to try deleting
        
        // Create a StorageSharedKeyCredential using your account name and key
        StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);

        // Define the SAS token parameters with Read permission (no delete)
        AccountSasPermissions permissions = AccountSasPermissions.Read | AccountSasPermissions.List; // Only Read permission
        
        //DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddHours(1);//1 hour 60 mins
        //DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddHours(.5);//half hour = 30 mins
        //DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddHours(.25);//15 mins
        //DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddHours(.123);//approx 7.5 mins
        DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddHours(.0625);//approx 3 mins

        // Create the SAS token builder
        AccountSasBuilder sasBuilder = new AccountSasBuilder()
        {
            Services = AccountSasServices.Blobs,  // Allow SAS for Blob services
            ResourceTypes = AccountSasResourceTypes.Service | AccountSasResourceTypes.Container | AccountSasResourceTypes.Object,  // Allow for container and blob-level access
            ExpiresOn = expiryTime,  // Expiry of the SAS token
        };

        // Set the permissions on the SAS token
        sasBuilder.SetPermissions(permissions);

        // Generate the SAS token
        string sasToken = sasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();
        
        // Construct the full URI with the SAS token
        Uri CompletesasUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}");
        Console.WriteLine("Complete SAS URI: " + CompletesasUri.ToString());
        string blobServiceURI = $"https://{accountName}.blob.core.windows.net";
        
        // Create a BlobServiceClient using the SAS URI
        BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri($"{blobServiceURI}?{sasToken}"));

        Console.WriteLine("*******************");
        // Call the method to list blobs in the container
        ListBlobsInContainer(blobServiceClient, containerName);
        Console.WriteLine("*******************");
        // Check if the SAS token is valid and print the time left
        CheckSasTokenValidity(sasBuilder);
        Console.WriteLine("*******************");
        // Try to delete a blob (should fail due to read-only permission)
        TryDeleteBlob(blobServiceClient, containerName, blobName);
    }

    // Method to list blobs in a container
    static void ListBlobsInContainer(BlobServiceClient blobServiceClient, string containerName)
    {
        try
        {
            // Get a reference to the container
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            Console.WriteLine($"Listing blobs in container: {containerName}");

            // List all blobs in the container
            foreach (var blobItem in containerClient.GetBlobs())
            {
                Console.WriteLine($"Blob name: {blobItem.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing blobs: {ex.Message}");
        }
    }

    // Method to check the SAS token validity
    static void CheckSasTokenValidity(AccountSasBuilder sasBuilder)
    {
        // Calculate the time left until the SAS token expires
        TimeSpan timeLeft = sasBuilder.ExpiresOn - DateTimeOffset.UtcNow;

        if (timeLeft.TotalSeconds > 0)
        {
            Console.WriteLine($"SAS Token is valid. Time left: {timeLeft.Hours} hours {timeLeft.Minutes} minutes.");
        }
        else
        {
            Console.WriteLine("SAS Token has expired.");
        }
    }

    // Method to attempt to delete a blob (should fail due to read-only permission)
    static void TryDeleteBlob(BlobServiceClient blobServiceClient, string containerName, string blobName)
    {
        try
        {
            // Get a reference to the container and the blob
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Try to delete the blob (this will fail due to read-only permission)
            Console.WriteLine($"Attempting to delete blob: {blobName}");
            blobClient.DeleteIfExists();  // This should fail because of the 'Read' permission
        }
        catch (Exception ex)
        {
            // This should fail with a permission error due to read-only SAS token
            Console.WriteLine($"Error attempting to delete blob"); //{ex.Message}
        }
    }
}
