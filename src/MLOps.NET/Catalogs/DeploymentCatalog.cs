﻿using MLOps.NET.Docker.Interfaces;
using MLOps.NET.Entities.Impl;
using MLOps.NET.Storage;
using MLOps.NET.Storage.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace MLOps.NET.Catalogs
{
    /// <summary>
    /// Exposes actions for deploying a model
    /// </summary>
    public sealed class DeploymentCatalog
    {
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IModelRepository modelRepository;
        private readonly IExperimentRepository experimentRepository;
        private readonly IDockerContext dockerContext;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="deploymentRepository"></param>
        /// <param name="modelRepository"></param>
        /// <param name="experimentRepository"></param>
        /// <param name="dockerContext"></param>
        public DeploymentCatalog(IDeploymentRepository deploymentRepository,
            IModelRepository modelRepository,
            IExperimentRepository experimentRepository,
            IDockerContext dockerContext)
        {
            this.deploymentRepository = deploymentRepository;
            this.modelRepository = modelRepository;
            this.experimentRepository = experimentRepository;
            this.dockerContext = dockerContext;
        }

        /// <summary>
        /// Creates a deployment target
        /// </summary>
        /// <param name="deploymentTargetName"></param>
        /// <param name="isProduction"></param>
        public async Task<DeploymentTarget> CreateDeploymentTargetAsync(string deploymentTargetName, bool isProduction = false)
        {
            return await this.deploymentRepository.CreateDeploymentTargetAsync(deploymentTargetName, isProduction);
        }

        /// <summary>
        /// Get deployment targets
        /// </summary>
        /// <returns></returns>
        public List<DeploymentTarget> GetDeploymentTargets()
        {
            return this.deploymentRepository.GetDeploymentTargets();
        }

        /// <summary>
        /// Deploys a registered model to a URI
        /// </summary>
        /// <param name="deploymentTarget"></param>
        /// <param name="registeredModel"></param>
        /// <param name="deployedBy"></param>
        /// <returns>A deployment</returns>
        /// <returns></returns>
        public async Task<Deployment> DeployModelAsync(DeploymentTarget deploymentTarget, RegisteredModel registeredModel, string deployedBy)
        {
            var experiment = this.experimentRepository.GetExperiment(registeredModel.ExperimentId);
            var deploymentUri = await this.modelRepository.DeployModelAsync(deploymentTarget, registeredModel, experiment);

            return await this.deploymentRepository.CreateDeploymentAsync(deploymentTarget, registeredModel, deployedBy, deploymentUri);
        }

        /// <summary>
        /// Deploys a model to a container in a cluster
        /// </summary>
        /// <param name="deploymentTarget"></param>
        /// <param name="registeredModel"></param>
        /// <param name="deployedBy"></param>
        /// <returns></returns>
        public async Task<Deployment> DeployModelToContainerAsync(DeploymentTarget deploymentTarget, RegisteredModel registeredModel, string deployedBy)
        {
            var experiment = this.experimentRepository.GetExperiment(registeredModel.ExperimentId);

            using var model = new MemoryStream();
            await modelRepository.DownloadModelAsync(registeredModel.RunId, model);

            await dockerContext.BuildImage(experiment.ExperimentName, registeredModel, model);
            await dockerContext.PushImage(experiment.ExperimentName, registeredModel);

            //Deploy to cluster

            var deploymentUri = "";

            return await this.deploymentRepository.CreateDeploymentAsync(deploymentTarget, registeredModel, deployedBy, deploymentUri);
        }

        /// <summary>
        /// Returns the URI to a deployed model
        /// </summary>
        /// <param name="experimentId"></param>
        /// <param name="deploymentTarget"></param>
        /// <returns></returns>
        public string GetDeploymentUri(Guid experimentId, DeploymentTarget deploymentTarget)
        {
            var experiment = this.experimentRepository.GetExperiment(experimentId);

            return this.modelRepository.GetDeploymentUri(experiment, deploymentTarget);
        }

        /// <summary>
        /// Gets deployments by experiment id
        /// </summary>
        /// <param name="experimentId"></param>
        /// <returns></returns>
        public List<Deployment> GetDeployments(Guid experimentId)
        {
            return this.deploymentRepository.GetDeployments(experimentId);
        }
    }
}
