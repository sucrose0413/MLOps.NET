﻿using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MLOps.NET.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MLOps.NET.AWS.Tests
{
    [TestClass]
    public class S3BucketModelRepositoryTests
    {
        [TestMethod]
        public async Task UploadModelAsync_ShouldSaveFileInS3Bucket()
        {
            // Arrange
            var mockAmzonClient = new Mock<IAmazonS3>();
            mockAmzonClient.Setup(a => a.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GetObjectMetadataResponse()
            {
                ContentLength = 100
            });
            var sut = new S3BucketModelRepository(mockAmzonClient.Object,"model-repository");

            // Act
            await sut.UploadModelAsync(new Guid(), "model.zip");

            // Assert
            mockAmzonClient.Verify(a => a.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task UploadModelAsync_ShouldCreateS3BucketIfNotExists()
        {
            // Arrange
            var mockAmzonClient = new Mock<IAmazonS3>();
            mockAmzonClient.Setup(a => a.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new AmazonS3Exception("Bucket does not exist"));

            var sut = new S3BucketModelRepository(mockAmzonClient.Object, "model-repository");

            // Act
            await sut.UploadModelAsync(new Guid(), "model.zip");

            // Assert
            mockAmzonClient.Verify(a => a.PutBucketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
