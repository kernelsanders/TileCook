﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TileCook.Web.Models.Config
{
    public class VectorTileLayerMap : IMap<Layer, LayerConfig>
    {
        private readonly IMap<IGridSet, GridSetConfig> _GridSetMap;
        private readonly IMap<ICache, CacheConfig> _CacheMap;
        private readonly IMap<IVectorTileProvider, ProviderConfig> _vectorTileProviderMap;
        private readonly IMap<Envelope, EnvelopeConfig> _EnvelopeMap;

        public VectorTileLayerMap (IMap<IGridSet, GridSetConfig> GridSetMap, 
            IMap<ICache, CacheConfig> CacheMap, 
            IMap<IVectorTileProvider, ProviderConfig> vectorTileProviderMap, 
            IMap<Envelope, EnvelopeConfig> EnvelopeMap)
        {
            this._GridSetMap = GridSetMap;
            this._CacheMap = CacheMap;
            this._vectorTileProviderMap = vectorTileProviderMap;
            this._EnvelopeMap = EnvelopeMap;
        }

        public Layer Map(LayerConfig obj)
        {
            if (obj != null)
            {
                return new Layer(
                    obj.Name,
                    obj.Title,
                    this._GridSetMap.Map(obj.Gridset),
                    this._CacheMap.Map(obj.Cache),
                    this._vectorTileProviderMap.Map(obj.Provider) as IProvider,
                    this._EnvelopeMap.Map(obj.Bounds),
                    obj.MinZoom,
                    obj.MaxZoom,
                    obj.Formats,
                    obj.BrowserCache,
                    obj.DisableCache,
                    obj.DisableProvider
                );
            }
            return null;
        }
    }
}