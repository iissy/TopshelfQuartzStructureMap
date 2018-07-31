using Quartz;
using StructureMap;
using System;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Quartz.StructureMap;
using Topshelf.StructureMap;

namespace TopshelfQuartzStructureMap
{
    class Program
    {
        static void Main()
        {
            HostFactory.Run(c =>
            {
                var container = new Container(cfg =>
                {
                    cfg.Scan(scan =>
                    {
                        scan.TheCallingAssembly();
                        scan.WithDefaultConventions();
                        scan.AssembliesFromApplicationBaseDirectory();
                    });
                    // 仅仅当接口与实现的命名不遵守约定才需要在这里注册，约定是接口的命名是在类命名前加多一个I
                    cfg.For<IOtherDependency>().Use<Dependency>();
                });
                // Init StructureMap container 
                c.UseStructureMap(container);

                c.Service<SampleService>(s =>
                {
                    //Construct topshelf service instance with StructureMap
                    s.ConstructUsingStructureMap();

                    s.WhenStarted((service, control) => service.Start());
                    s.WhenStopped((service, control) => service.Stop());

                    //Construct IJob instance with StructureMap
                    s.UseQuartzStructureMap();

                    s.ScheduleQuartzJob(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<SampleJob>().Build())
                            .AddTrigger(() =>
                                TriggerBuilder.Create()
                                    .WithSimpleSchedule(builder => builder
                                                                    .WithIntervalInSeconds(5)
                                                                    .RepeatForever())
                                                                    .Build())
                        );
                });
            });
        }
    }

    public interface IOtherDependency
    {
        void Write();
    }

    public class Dependency : IOtherDependency
    {
        public void Write()
        {
            Console.WriteLine("Line from Service dependency");
        }
    }

    internal class SampleService
    {
        private readonly IOtherDependency _dependency;
        public SampleService(IOtherDependency dependency)
        {
            _dependency = dependency;
        }

        public bool Start()
        {
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Sample Service Started...");
            _dependency.Write();
            Console.WriteLine("--------------------------------");
            return true;
        }

        public bool Stop()
        {
            return true;
        }
    }

    internal class SampleJob : IJob
    {
        private readonly IOtherDependency _dependency;

        public SampleJob(IOtherDependency dependency)
        {
            _dependency = dependency;
        }

        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("{0} - Sample job executing...", DateTime.Now);
            _dependency.Write();
            Console.WriteLine("Sample job executed.");
            return Task.FromResult(true);
        }
    }
}