// Cloudinary Image Management Functions
function deleteProductImage(imageId, button) {
    if (confirm('Are you sure you want to delete this image?')) {
        fetch('/Product/DeleteImage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ imageId: imageId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Remove the image container from the DOM
                const imageContainer = button.closest('.product-image-container');
                if (imageContainer) {
                    imageContainer.remove();
                }
            } else {
                alert('Error deleting image: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred while deleting the image.');
        });
    }
}

// Preview images before upload
function previewImages() {
    const preview = document.getElementById('imagePreview');
    if (!preview) return;
    
    preview.innerHTML = '';
    const files = document.querySelector('input[type=file]').files;

    if (files.length === 0) {
        preview.innerHTML = '<p>No images selected</p>';
        return;
    }

    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        if (!file.type.startsWith('image/')) {
            continue;
        }
        
        const reader = new FileReader();
        
        reader.onload = function(e) {
            const div = document.createElement('div');
            div.className = 'product-image-preview';
            div.innerHTML = `
                <img src="${e.target.result}" alt="Image preview" />
                <span>${file.name}</span>
            `;
            preview.appendChild(div);
        }
        
        reader.readAsDataURL(file);
    }
}
