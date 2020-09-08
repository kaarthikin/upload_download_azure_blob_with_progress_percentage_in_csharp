// www.craftedforeveryone.com
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  

using Azure.Storage.Blobs;
using ByteSizeLib;
using ShellProgressBar;
using System;
using System.IO;

namespace UploadDownloadAzureBlobWithProgressSDK12
{
    public class BlobStorageService
    {
        string connectionString;
        string containerName;

        ProgressBar progressBar;
        long uploadFileSize;

        public BlobStorageService(string connectionString)
        {
            this.connectionString = connectionString;
            containerName = "craftedforeveryone";
        }

        public void UploadBlob(string fileToUploadPath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Uploading File From : {fileToUploadPath}\n");

            var file = new FileInfo(fileToUploadPath);
            uploadFileSize = file.Length; //Get the file size. This is need to calculate the file upload progress

            //Initialize the progress bar. ShellProgress bar NuGet Package is used here
            progressBar = new ProgressBar(100, "Upload File Progress", new ProgressBarOptions
            {
                ForegroundColor=ConsoleColor.Cyan
            });

            //Initialize a progress handler. When the file is being uploaded, the current uploaded bytes will be published back to us using this progress handler by the Blob Storage Service
            var progressHandler = new Progress<long>();
            progressHandler.ProgressChanged += UploadProgressChanged;


            var blob = new BlobClient(connectionString, containerName, file.Name); //Initialize the blob client
            blob.Upload(fileToUploadPath, progressHandler: progressHandler); //Make use to pass the progress handler here

            progressBar.Dispose(); //Dispose the progress bar once uploading is completed
            PrintLine();
        }

        public void DownloadBlob(string blobPath,string pathToDownload)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Downloading File \"{blobPath}\" To \"{pathToDownload}\"\n");

            //Initialize the progress bar. ShellProgress bar NuGet Package is used here
            progressBar = new ProgressBar(100, "Download File Progress", new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green
            });

            //Get the blob client instance and call the method Download().
            //Note: At this line we are not download the file, we are just getting the download stream of the file with additional metadata to download.
            var blobToDownload = new BlobClient(connectionString, containerName, blobPath).Download().Value;
            var outputFile = File.OpenWrite(pathToDownload);

            //Choose an appropriate buffer size
            var downloadBuffer = new byte[81920];
            int bytesRead;
            int totalBytesDownloaded=0;
            
            //Read(Download) the file in bytes
            while((bytesRead=blobToDownload.Content.Read(downloadBuffer,0,downloadBuffer.Length))!=0)
            {
                outputFile.Write(downloadBuffer, 0, bytesRead); // Write the download bytes from source stream to destination stream.
                totalBytesDownloaded += bytesRead;//Increment the total downloaded counter. This is used for percentage calculation

                //Report the process to progress bar
                progressBar.Tick((int)GetProgressPercentage(blobToDownload.ContentLength, totalBytesDownloaded),
                    $"Downloaded {ByteSize.FromBytes(totalBytesDownloaded).MebiBytes:#.##} MB of {ByteSize.FromBytes(blobToDownload.ContentLength).MebiBytes:#.##} MB");
            }

            //Close both the source and destination stream
            blobToDownload.Content.Close();
            outputFile.Close();

            progressBar.Dispose();

            PrintLine();
        }


        private double GetProgressPercentage(double totalSize,double currentSize)
        {
            return (currentSize / totalSize) * 100;
        }

        private void UploadProgressChanged(object sender, long bytesUploaded)
        {
            //Calculate the progress and update the progress bar.
            //Note: the bytes uploaded published back to us is in long. In order to calculate the percentage, the value has to be converted to double. 
            //Auto type casting from long to double happens here as part of function call
            progressBar.Tick((int)GetProgressPercentage(uploadFileSize, bytesUploaded),
                $"Uploaded {ByteSize.FromBytes(bytesUploaded).MebiBytes:#.##} MB of {ByteSize.FromBytes(uploadFileSize).MebiBytes:#.##} MB");
        }


        static void PrintLine()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            for (int i=0;i<120;i++)
            {
                Console.Write("=");
            }
            Console.WriteLine();
        }
        static void DisplayHeader()
        {
            PrintLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"

    ___           __ _           _   ___           __                                                            
   / __\ __ __ _ / _| |_ ___  __| | / __\__  _ __ /__\_   _____ _ __ _   _  ___  _ __   ___   ___ ___  _ __ ___  
  / / | '__/ _` | |_| __/ _ \/ _` |/ _\/ _ \| '__/_\ \ \ / / _ \ '__| | | |/ _ \| '_ \ / _ \ / __/ _ \| '_ ` _ \ 
 / /__| | | (_| |  _| ||  __/ (_| / / | (_) | | //__  \ V /  __/ |  | |_| | (_) | | | |  __/| (_| (_) | | | | | |
 \____/_|  \__,_|_|  \__\___|\__,_\/   \___/|_| \__/   \_/ \___|_|   \__, |\___/|_| |_|\___(_)___\___/|_| |_| |_|
                                                                     |___/                                       
            ");
            Console.WriteLine("Upload / Download File To / From Blob Storage With Progress Percentage Using SDK v12");
            Console.ForegroundColor = ConsoleColor.Gray;

            PrintLine();
            Console.WriteLine("\n");
        }

        static void Main(string[] args)
        {
            DisplayHeader();

            BlobStorageService blobStorageService = new BlobStorageService("AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;");
            blobStorageService.UploadBlob("C:\\Temp\\upload.msi");
            blobStorageService.DownloadBlob("upload.msi", "c:\\temp\\download_upload.msi");

            Console.ReadLine();
        }
    }
}
