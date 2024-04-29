using System.Collections.Generic;

namespace Engine
{
    public class GameGraphicsSettings
    {
        public readonly Dictionary<string, List<string>> Pipelines = new Dictionary<string, List<string>>();

        public void Load()
        {
            foreach (KeyValuePair<string, List<string>> pipeline in Pipelines)
            {
                ShaderPipeline.LoadPipeline(pipeline.Key, pipeline.Value);
            }
        }

        public void Unload()
        {
            foreach (string pipelineName in Pipelines.Keys)
            {
                ShaderPipeline.UnloadPipeline(pipelineName);
            }
        }

        public static GameGraphicsSettings Default()
        {
            return new GameGraphicsSettings()
            {
                Pipelines =
                {
                    {
                        "depth_only", new List<string>()
                        {
                            @"BaseAssets\Shaders\depth_only.vsh",
                            @"BaseAssets\Shaders\depth_only.fsh"
                        }
                    },
                    {
                        "deferred_geometry", new List<string>()
                        {
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry.fsh"
                        }
                    },
                    {
                        "deferred_geometry_skinned", new List<string>()
                        {
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry_skinned.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry.fsh"
                        }
                    },
                    {
                        "deferred_geometry_particles", new List<string>()
                        {
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry_particles.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry_particles.gsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_geometry_particles.fsh"
                        }
                    },
                    {
                        "deferred_light_point", new List<string>()
                        {
                            @"BaseAssets\Shaders\screen_quad.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_light_point.fsh"
                        }
                    },
                    {
                        "deferred_light_directional", new List<string>()
                        {
                            @"BaseAssets\Shaders\screen_quad.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_light_directional.fsh"
                        }
                    },
                    {
                        "deferred_addLight", new List<string>()
                        {
                            @"BaseAssets\Shaders\screen_quad.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deffered_addLight.fsh"
                        }
                    },
                    {
                        "deferred_gamma_correction", new List<string>()
                        {
                            @"BaseAssets\Shaders\screen_quad.vsh",
                            @"BaseAssets\Shaders\DeferredRender\deferred_gamma_correction.fsh"
                        }
                    },
                    {
                        "FEM_gas_volume", new List<string>()
                        {
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume.vsh",
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume.fsh"
                        }
                    },
                    {
                        "FEM_gas_volume_octree", new List<string>()
                        {
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume_octree.vsh",
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume_debug.fsh"
                        }
                    },
                    {
                        "FEM_gas_volume_tetrahedrons", new List<string>()
                        {
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume_tetrahedrons.vsh",
                            @"BaseAssets\Shaders\Volumetric\FEM_gas_volume_debug.fsh"
                        }
                    },
                }
            };
        }
    }
}