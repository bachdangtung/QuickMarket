// product-favorites.js - Handle favorite product functionality

$(document).ready(function() {
    // Check if product is in favorites when page loads
    if ($('#favoriteBtn').length) {
        const productId = $('#favoriteBtn').data('product-id');
        checkIsFavorite(productId);
    }
    
    // Toggle favorite status when button is clicked
    $('#favoriteBtn').on('click', function() {
        const productId = $(this).data('product-id');
        const isFavorite = $(this).hasClass('favorite-active');
        
        if (isFavorite) {
            removeFromFavorites(productId);
        } else {
            addToFavorites(productId);
        }
    });
});

// Check if product is in user's favorites
function checkIsFavorite(productId) {
    $.ajax({
        url: `/Product/IsFavorite?productId=${productId}`,
        type: 'GET',
        success: function(response) {
            if (response.isFavorite) {
                $('#favoriteBtn').addClass('favorite-active');
                $('#favoriteBtn').html('<i class="fas fa-heart"></i> Đã yêu thích');
            } else {
                $('#favoriteBtn').removeClass('favorite-active');
                $('#favoriteBtn').html('<i class="far fa-heart"></i> Yêu thích');
            }
        },
        error: function() {
            console.error('Lỗi khi kiểm tra trạng thái yêu thích');
        }
    });
}

// Add product to favorites
function addToFavorites(productId) {
    $.ajax({
        url: '/Product/AddFavorite',
        type: 'POST',
        data: { productId: productId },
        success: function(response) {
            if (response.success) {
                $('#favoriteBtn').addClass('favorite-active');
                $('#favoriteBtn').html('<i class="fas fa-heart"></i> Đã yêu thích');
                toastr.success('Đã thêm vào danh sách yêu thích');
            } else {
                toastr.error(response.message || 'Không thể thêm vào danh sách yêu thích');
            }
        },
        error: function() {
            toastr.error('Có lỗi xảy ra. Vui lòng thử lại sau.');
        }
    });
}

// Remove product from favorites
function removeFromFavorites(productId) {
    $.ajax({
        url: '/Product/RemoveFavorite',
        type: 'POST',
        data: { productId: productId },
        success: function(response) {
            if (response.success) {
                $('#favoriteBtn').removeClass('favorite-active');
                $('#favoriteBtn').html('<i class="far fa-heart"></i> Yêu thích');
                toastr.success('Đã xóa khỏi danh sách yêu thích');
            } else {
                toastr.error(response.message || 'Không thể xóa khỏi danh sách yêu thích');
            }
        },
        error: function() {
            toastr.error('Có lỗi xảy ra. Vui lòng thử lại sau.');
        }
    });
}
