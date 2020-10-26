using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using AutoMapper;

namespace STak.TakHub.Interop
{
    public class Mapper
    {
        public static IMapper AutoMapper { get; }


        static Mapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TimeSpan, double>().ConvertUsing(t => t.TotalMilliseconds);
                cfg.CreateMap<double, TimeSpan>().ConvertUsing(d => TimeSpan.FromMilliseconds(d));

                cfg.AddProfile<TakHubMappingProfile>();
            });

            try
            {
                config.AssertConfigurationIsValid();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            AutoMapper = config.CreateMapper();
        }


        public static TDestination Map<TDestination>(object obj) => AutoMapper.Map<TDestination>(obj);
    }
}
