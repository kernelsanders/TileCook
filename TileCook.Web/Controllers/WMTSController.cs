﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using TileCook.Web.WMTSService;
using System.Xml.Serialization;

namespace TileCook.Web.Controllers
{
    public class WMTSController : ApiController
    {
        [HttpGet]
        [ActionName("GetTile")]
        public HttpResponseMessage GetTile(string Version, string Layer, string Style, string TileMatrixSet, int TileMatrix, int TileRow, int TileCol, string Format)
        {
            // Validate version
            if (!Version.Equals("1.0.0", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            // Validate layer
            Layer layer = LayerCache.GetLayer(Layer);
            if (layer == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            // Validate style
            if (!Style.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!TileMatrixSet.Equals(layer.gridset.name, StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            // Flip Y
            TileCol = layer.gridset.gridHeight(TileMatrix) - TileCol - 1;

            // Get image
            byte[] img;
            try
            {
                img = layer.getTile(TileMatrix, TileRow, TileCol, Format);
            }
            catch (TileOutOfRangeException)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidTileFormatException)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            // Start response
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new ByteArrayContent(img);

            // Set content type
            string mimeMapping = System.Web.MimeMapping.GetMimeMapping("." + Format);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeMapping);

            // Set browser cache control
            response.Headers.CacheControl = new CacheControlHeaderValue();
            response.Headers.CacheControl.MaxAge = TimeSpan.FromSeconds(layer.browserCache);

            return response;

        }

        [HttpGet]
        [ActionName("GetCapabilities")]
        public HttpResponseMessage GetCapabilities(string Version)
        {
            // Validate version
            if (!Version.Equals("1.0.0", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            List<Layer> layers = new List<Layer>(LayerCache.GetLayers().Values);
            Dictionary<string, GridSet> uniqueGridSets = new Dictionary<string, GridSet>();
            foreach (Layer l in layers)
            {
                if (!uniqueGridSets.ContainsKey(l.gridset.name))
                {
                    uniqueGridSets[l.gridset.name] = l.gridset;
                }
            }
            List<GridSet> gridSets = new List<GridSet>(uniqueGridSets.Values);
            
            // Start capabilities 
            Capabilities capabilities = new Capabilities();
            capabilities.version = "1.0.0";
            
            // ServiceMetadataURL is only set for REST
            capabilities.ServiceMetadataURL = new OnlineResourceType[]{ new OnlineResourceType() };
            capabilities.ServiceMetadataURL[0].href = Url.Link("WMTSGetCapabilities", new { Version = "1.0.0" });

            // ServiceIdentification
            capabilities.ServiceIdentification = new ServiceIdentification();
            capabilities.ServiceIdentification.Title = new LanguageStringType[] {new LanguageStringType()};
            capabilities.ServiceIdentification.Title[0].Value = "Web Map Tile Service";
            capabilities.ServiceIdentification.ServiceType = new CodeType();
            capabilities.ServiceIdentification.ServiceType.Value = "OGC WMTS";
            capabilities.ServiceIdentification.ServiceTypeVersion = new string[] { "1.0.0" }; 

            // ServiceProvider
            capabilities.ServiceProvider = null;

            // Operations metadata only needed for KVP or SOAP
            capabilities.OperationsMetadata = null;

            // Contents
            capabilities.Contents = new ContentsType();
            capabilities.Contents.DatasetDescriptionSummary = new LayerType[layers.Count];
            for(int i=0; i<layers.Count;i++)
            {
                capabilities.Contents.DatasetDescriptionSummary[i] = new LayerType();
                LayerType LayerType = (LayerType)capabilities.Contents.DatasetDescriptionSummary[i];
                LayerType.Identifier = new CodeType();
                LayerType.Identifier.Value = layers[i].name;
                LayerType.Title = new LanguageStringType[] { new LanguageStringType() };
                LayerType.Title[0].Value = layers[i].Title;
                LayerType.BoundingBox = new BoundingBoxType[] { new BoundingBoxType() };
                LayerType.BoundingBox[0].crs = layers[i].gridset.srs;
                LayerType.BoundingBox[0].LowerCorner = layers[i].bounds.minx.ToString() + " " + layers[i].bounds.miny.ToString();
                LayerType.BoundingBox[0].UpperCorner = layers[i].bounds.maxx.ToString() + " " + layers[i].bounds.maxy.ToString();
                LayerType.Style = new Style[] {new Style() };
                LayerType.Style[0].isDefault = true;
                LayerType.Style[0].Identifier = new CodeType();
                LayerType.Style[0].Identifier.Value = "default";
                LayerType.Format = new string[layers[i].formats.Count];
                for (int j = 0; j < layers[i].formats.Count;j++ )
                {
                    LayerType.Format[j] = System.Web.MimeMapping.GetMimeMapping("." + layers[i].formats[j]);
                }
                LayerType.TileMatrixSetLink = new TileMatrixSetLink[] { new TileMatrixSetLink() };
                LayerType.TileMatrixSetLink[0].TileMatrixSet = layers[i].gridset.name;
                if (!layers[i].bounds.Equals(layers[i].gridset.envelope))
                {
                    int zLevels = layers[i].maxZoom - layers[i].minZoom;
                    LayerType.TileMatrixSetLink[0].TileMatrixSetLimits = new TileMatrixLimits[zLevels];
                    for (int j = 0; j < zLevels; j++)
                    {
                        int z = layers[i].minZoom + j;
                        Grid g = layers[i].gridset.grids[z];
                        Tile lowTile = layers[i].gridset.PointToTile(new Point(layers[i].bounds.minx, layers[i].bounds.miny), z);
                        Tile highTile = layers[i].gridset.PointToTile(new Point(layers[i].bounds.maxx, layers[i].bounds.maxy), z);
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j] = new TileMatrixLimits();
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j].TileMatrix = (layers[i].minZoom + j).ToString();
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j].MinTileRow = lowTile.x.ToString();
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j].MaxTileRow = highTile.x.ToString();
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j].MinTileCol = lowTile.y.ToString();
                        LayerType.TileMatrixSetLink[0].TileMatrixSetLimits[j].MaxTileCol = highTile.y.ToString();
                    }
                }
                LayerType.ResourceURL = new URLTemplateType[layers[i].formats.Count];
                for (int j = 0; j < layers[i].formats.Count; j++)
                {
                    LayerType.ResourceURL[j] = new URLTemplateType();
                    LayerType.ResourceURL[j].format = System.Web.MimeMapping.GetMimeMapping("." + layers[i].formats[j]);
                    LayerType.ResourceURL[j].resourceType = URLTemplateTypeResourceType.tile;
                    LayerType.ResourceURL[j].template = HttpUtility.UrlDecode(Url.Link("WMTSGetTile", new { 
                        Version = "1.0.0", 
                        Layer = layers[i].name, 
                        Style = "default", 
                        TileMatrixSet = layers[i].gridset.name,
                        TileMatrix = "{TileMatrix}",
                        TileRow = "{TileRow}",
                        TileCol = "{TileCol}",
                        Format = layers[i].formats[j]
                    }));
                }
            }
            capabilities.Contents.TileMatrixSet = new TileMatrixSet[gridSets.Count];
            for (int i = 0; i < gridSets.Count; i++)
            {
                capabilities.Contents.TileMatrixSet[i] = new TileMatrixSet();
                capabilities.Contents.TileMatrixSet[i].Identifier = new CodeType();
                capabilities.Contents.TileMatrixSet[i].Identifier.Value = gridSets[i].name;
                capabilities.Contents.TileMatrixSet[i].SupportedCRS = gridSets[i].srs;
                if (gridSets[i].name.Equals("GoogleMapsCompatible", StringComparison.OrdinalIgnoreCase) ||
                    gridSets[i].name.Equals("GlobalCRS84Scale", StringComparison.OrdinalIgnoreCase) ||
                    gridSets[i].name.Equals("GlobalCRS84Pixel", StringComparison.OrdinalIgnoreCase) ||
                    gridSets[i].name.Equals("GGoogleCRS84Quad", StringComparison.OrdinalIgnoreCase)
                )
                {
                    capabilities.Contents.TileMatrixSet[i].WellKnownScaleSet = gridSets[i].name;
                }
                capabilities.Contents.TileMatrixSet[i].TileMatrix = new TileMatrix[gridSets[i].grids.Count];
                for (int j = 0; j < gridSets[i].grids.Count; j++)
                {
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j] = new TileMatrix();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].ScaleDenominator = gridSets[i].grids[j].scale;
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].Identifier = new CodeType();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].Identifier.Value = gridSets[i].grids[j].name;
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].TopLeftCorner = gridSets[i].envelope.minx.ToString() + " " + gridSets[i].envelope.maxy.ToString();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].TileWidth = gridSets[i].tileWidth.ToString();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].TileHeight = gridSets[i].tileHeight.ToString();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].MatrixWidth = gridSets[i].gridWidth(j).ToString();
                    capabilities.Contents.TileMatrixSet[i].TileMatrix[j].MatrixHeight = gridSets[i].gridHeight(j).ToString();
                }
            }

            // Serialize 
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "http://www.opengis.net/wmts/1.0");
            ns.Add("xlink", "http://www.w3.org/1999/xlink");
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");
            ns.Add("gml", "http://www.opengis.net/gml");

            return Request.CreateResponse(HttpStatusCode.OK, capabilities, new ExtendedXmlMediaTypeFormatter(ns));
        }
    }
}