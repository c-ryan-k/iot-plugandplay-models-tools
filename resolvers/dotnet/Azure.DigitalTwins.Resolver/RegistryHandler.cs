﻿using Azure.DigitalTwins.Resolver.Fetchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.DigitalTwins.Resolver
{
    public class RegistryHandler
    {
        public enum RegistryTypeCategory
        {
            RemoteUri,
            LocalUri
        }

        public Uri RegistryUri { get; }
        public RegistryTypeCategory RegistryType { get; }

        private readonly IModelFetcher _modelFetcher;
        private readonly ModelQuery _modelQuery;

        public RegistryHandler(Uri registryUri)
        {
            _modelQuery = new ModelQuery();
            RegistryUri = registryUri;
            if (registryUri.Scheme == "file")
            {
                this.RegistryType = RegistryTypeCategory.LocalUri;
                this._modelFetcher = new LocalModelFetcher();
            }
            else
            {
                this.RegistryType = RegistryTypeCategory.RemoteUri;
                this._modelFetcher = new RemoteModelFetcher();
            }
        }

        public async Task<IDictionary<string, string>> ProcessAsync(string dtmi, bool includeDepencies = true)
        {
            return await this.ProcessAsync(new List<string>() { dtmi }, includeDepencies);
        }

        public async Task<IDictionary<string, string>> ProcessAsync(IEnumerable<string> dtmis, bool includeDependencies = true)
        {
            Dictionary<string, string> processedModels = new Dictionary<string, string>();
            Queue<string> toProcessModels = new Queue<string>();

            foreach (string dtmi in dtmis)
            {
                toProcessModels.Enqueue(dtmi);
            }

            while (toProcessModels.Count != 0)
            {
                string targetDtmi = toProcessModels.Dequeue();
                if (processedModels.ContainsKey(targetDtmi))
                {
                    continue;
                }

                string definition = await this.FetchAsync(targetDtmi);

                if (includeDependencies)
                {
                    List<string> dependencies = this._modelQuery.GetDependencies(definition);
                    foreach (string dep in dependencies)
                    {
                        toProcessModels.Enqueue(dep);
                    }
                }

                processedModels.Add(targetDtmi, definition);
            }

            return processedModels;
        }

        private async Task<string> FetchAsync(string dtmi)
        {
            return await this._modelFetcher.FetchAsync(dtmi, this.RegistryUri);
        }
    }
}
