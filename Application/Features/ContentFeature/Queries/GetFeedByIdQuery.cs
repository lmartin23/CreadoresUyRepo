﻿using Application.Interface;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Share;
using Share.Dtos;
using Share.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CreatorFeatures.Queries
{
    public class GetFeedByIdQuery : IRequest<Response<List<ContentDto>>>
    {
        public int IdUser {  get; set; }
        public int IdCreator { get; set; }

        public int Page { get; set; }
        public int ContentPerPage { get; set; }


        public class GetFeedByIdQueryHandler : IRequestHandler<GetFeedByIdQuery, Response<List<ContentDto>>>
        {
            private readonly ICreadoresUyDbContext _context;
            private readonly IMapper _mapper;

            public GetFeedByIdQueryHandler(ICreadoresUyDbContext context, IMapper mapper)
            {
                _context = context;
                _mapper = mapper;

            }
            public async Task<Response<List<ContentDto>>> Handle(GetFeedByIdQuery query, CancellationToken cancellationToken)
            {
                var idPlans = await _context.UserPlans.Include(up=> up.Plan).Where(up => up.IdUser == query.IdUser && up.ExpirationDate >= DateTime.Now && !up.Plan.Deleted&& !up.Deleted ).ToListAsync();
                var listPlans = new List<int>();
                foreach (var idPlan in idPlans) {

                    listPlans.Add(idPlan.IdPlan);
                }
                var content = await _context.Contents.Include(c=>c.ContentPlans).ThenInclude(cp=>cp.Plan).Include(c=> c.ContentPlans).ThenInclude(cp=>cp.Plan).ThenInclude(p=>p.Creator)
                    .Where(c =>c.ContentPlans.Any(cp=> cp.Plan.Creator.Id == query.IdCreator) && c.ContentPlans.Any(cp=>listPlans.Contains(cp.IdPlan) && cp.Plan.Deleted == false && cp.Content.Draft == false) 
                    || (c.IsPublic && c.ContentPlans.Any(cp=>cp.Plan.Creator.Id == query.IdCreator)) && !c.Deleted  ).OrderByDescending(c=>c.PublishDate).Skip(query.Page*query.ContentPerPage).Take(query.ContentPerPage).ToListAsync();
                List<ContentDto> list = new List<ContentDto>();

                content.ForEach(async x => {
                    int creadorId = x.ContentPlans.FirstOrDefault().Plan.CreatorId;
                    Creator creator = _context.Creators.Where(c=> c.Id ==creadorId).FirstOrDefault();
                    ContentDto contentDataBaseDto = _mapper.Map<ContentDto>(x);
                    contentDataBaseDto.IdCreator = creadorId;
                    contentDataBaseDto.NickName = creator.NickName;
                    contentDataBaseDto.NoNulls();
                    list.Add(contentDataBaseDto); 
                });
                Response<List<ContentDto>> res = new Response<List<ContentDto>>
                {
                    Message = new List<String>
                    {
                        "Lista De Feed"
                    },
                    Success = true,
                    CodStatus = System.Net.HttpStatusCode.OK,
                    Obj = list
                };
                return res;
            }
        }
    }
}

