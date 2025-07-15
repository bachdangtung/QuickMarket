using AutoMapper;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.DTOs.Users;
using BussinessLogic.Models;
using QuickMarket.Models;

namespace QuickMarket.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity to DTO mappings (Model -> DTO)
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ProductImages.Select(img => img.ImageUrl)));
            
            // DTO to Entity mappings (DTO -> Model)
            CreateMap<ProductDto, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ProductReviews, opt => opt.Ignore())
                .ForMember(dest => dest.Favorites, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<ProductReview, ProductReviewDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.Replies, opt => opt.Ignore()); // Custom mapping needed

            CreateMap<ProductCategory, CategoryDto>();
            CreateMap<CategoryDto, ProductCategory>();

            CreateMap<ProductImage, ProductImageDto>();

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));
            
            CreateMap<ExternalLogin, ExternalLoginDto>();

            // ViewModel to DTO mappings (ViewModel -> DTO)
            CreateMap<ProductViewModel, ProductDto>()
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ExistingImageUrls))
                .ForMember(dest => dest.Reviews, opt => opt.Ignore());

            // DTO to ViewModel mappings (DTO -> ViewModel)
            CreateMap<ProductDto, ProductViewModel>()
                .ForMember(dest => dest.ExistingImageUrls, opt => opt.MapFrom(src => src.ImageUrls));
            
            CreateMap<ProductReviewDto, ProductReviewViewModel>();
            CreateMap<ProductReviewViewModel, ProductReviewDto>();
            
            // Removed direct Model <-> ViewModel mappings for clarity and proper separation
        }
    }
}
