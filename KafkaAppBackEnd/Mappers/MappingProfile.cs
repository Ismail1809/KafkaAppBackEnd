﻿using AutoMapper;
using KafkaAppBackEnd.Models;
using static System.Net.Mime.MediaTypeNames;

namespace KafkaAppBackEnd.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UpdateConnectionRequest, Connection>();
            CreateMap<CreateConnectionRequest, Connection>();
        }
    }
}
