using AutoMapper;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.DTOs.Users;
using BussinessLogic.Models;

namespace QuickMarket.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity to DTO mappings (Model -> DTO)
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : null))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ProductImages.Select(img => img.ImageUrl)))
                .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.ProductReviews));
            
            // DTO to Entity mappings (DTO -> Model)
            CreateMap<ProductDto, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ProductReviews, opt => opt.Ignore())
                .ForMember(dest => dest.Favorites, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<ProductReview, ProductReviewDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.Replies, opt => opt.Ignore()); // Custom mapping needed

            CreateMap<ProductCategory, CategoryDto>();
            CreateMap<CategoryDto, ProductCategory>();

            CreateMap<ProductImage, ProductImageDto>();

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));

            // ProductCreateUpdateDto mappings
            CreateMap<ProductCreateUpdateDto, ProductDto>();
            CreateMap<ProductDto, ProductCreateUpdateDto>()
                .ForMember(dest => dest.ExistingImageUrls, opt => opt.MapFrom(src => src.ImageUrls));
            CreateMap<ProductCreateUpdateDto, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ProductReviews, opt => opt.Ignore())
                .ForMember(dest => dest.Favorites, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());
        }
    }
}

