using System.Collections.Generic;

using Engine;

namespace Editor.GameProject
{
    public class ProjectGraphicsSettings
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

        public static ProjectGraphicsSettings Default()
        {
            return new ProjectGraphicsSettings()
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
                        "volume", new List<string>()
                        {
                            @"BaseAssets\Shaders\VolumetricRender\volume.vsh",
                            @"BaseAssets\Shaders\VolumetricRender\volume.fsh"
                        }
                    },
                }
            };
        }
    }
}