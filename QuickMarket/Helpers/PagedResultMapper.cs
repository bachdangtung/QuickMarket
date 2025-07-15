using AutoMapper;
using BussinessLogic.DTOs;
using System.Collections.Generic;

namespace QuickMarket.Helpers
{
    public static class PagedResultMapper
    {
        public static PagedResult<TDestination> ToMappedPagedResult<TSource, TDestination>(this PagedResult<TSource> pagedResult, IMapper mapper)
        {
            return new PagedResult<TDestination>
            {
                Items = mapper.Map<List<TDestination>>(pagedResult.Items),
                TotalCount = pagedResult.TotalCount,
                PageCount = pagedResult.PageCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
        }
    }
}
