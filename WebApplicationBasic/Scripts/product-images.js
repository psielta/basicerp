/**
 * Product Images Management
 * Handles image upload, deletion, reordering, and setting main image
 * Version: 1.0
 */

(function ($) {
    'use strict';

    // Allowed image extensions
    var allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
    var maxFileSize = 5 * 1024 * 1024; // 5MB

    /**
     * Initialize the image management functionality
     */
    function init() {
        // Load existing images for each variant
        $('.variant-images-section').each(function () {
            var variantId = $(this).data('variant-id');
            if (variantId) {
                loadVariantImages(variantId);
            }
        });

        // Initialize sortable for image grids
        initSortable();

        // Initialize upload zones
        initUploadZones();

        // Initialize action handlers
        initActionHandlers();
    }

    /**
     * Load images for a variant
     */
    function loadVariantImages(variantId) {
        $.ajax({
            url: '/Products/GetVariantImages',
            type: 'GET',
            data: { variantId: variantId },
            success: function (response) {
                if (response.success) {
                    renderImages(variantId, response.images);
                }
            },
            error: function () {
                console.error('Erro ao carregar imagens da variante:', variantId);
            }
        });
    }

    /**
     * Render images in the grid
     */
    function renderImages(variantId, images) {
        var $grid = $('#images-grid-' + variantId);
        $grid.empty();

        if (images && images.length > 0) {
            images.forEach(function (img) {
                var $item = createImageItem(img);
                $grid.append($item);
            });

            // Update image count
            updateImageCount(variantId, images.length);
        }

        // Reinitialize sortable
        initSortableForGrid($grid);
    }

    /**
     * Create an image item element
     */
    function createImageItem(img) {
        var mainClass = img.IsMain ? 'is-main' : '';
        var mainBadge = img.IsMain ? '<span class="main-badge">Principal</span>' : '';
        var setMainBtn = img.IsMain ? '' : '<button type="button" class="btn-set-main" data-image-id="' + img.Id + '">Principal</button>';

        var html = '<div class="image-item ' + mainClass + '" data-image-id="' + img.Id + '">' +
            mainBadge +
            '<img src="' + img.Url + '" alt="' + (img.AltText || '') + '" />' +
            '<div class="image-overlay">' +
            '<div class="image-actions">' +
            setMainBtn +
            '<button type="button" class="btn-delete-image" data-image-id="' + img.Id + '">Remover</button>' +
            '</div>' +
            '</div>' +
            '</div>';

        return $(html);
    }

    /**
     * Update image count badge
     */
    function updateImageCount(variantId, count) {
        var $section = $('.variant-images-section[data-variant-id="' + variantId + '"]');
        var $badge = $section.find('.images-count');
        if ($badge.length) {
            $badge.text(count + ' ' + (count === 1 ? 'imagem' : 'imagens'));
        }
    }

    /**
     * Initialize sortable for all grids
     */
    function initSortable() {
        $('.images-grid.sortable').each(function () {
            initSortableForGrid($(this));
        });
    }

    /**
     * Initialize sortable for a specific grid
     */
    function initSortableForGrid($grid) {
        if ($grid.hasClass('ui-sortable')) {
            $grid.sortable('destroy');
        }

        $grid.sortable({
            items: '.image-item',
            placeholder: 'image-item ui-sortable-placeholder',
            tolerance: 'pointer',
            cursor: 'move',
            update: function (event, ui) {
                saveImageOrder($grid);
            }
        });
    }

    /**
     * Save image order after sorting
     */
    function saveImageOrder($grid) {
        var imageIds = [];
        $grid.find('.image-item').each(function () {
            var id = $(this).data('image-id');
            if (id) {
                imageIds.push(id);
            }
        });

        if (imageIds.length === 0) return;

        $.ajax({
            url: '/Products/ReorderImages',
            type: 'POST',
            data: { imageIds: imageIds },
            traditional: true,
            success: function (response) {
                if (response.success) {
                    showToast('Ordem das imagens atualizada', 'success');
                } else {
                    showToast(response.message || 'Erro ao reordenar', 'error');
                }
            },
            error: function () {
                showToast('Erro ao reordenar imagens', 'error');
            }
        });
    }

    /**
     * Initialize upload zones
     */
    function initUploadZones() {
        // Click to open file dialog
        $('.upload-zone').on('click', function (e) {
            if ($(e.target).is('input')) return;
            $(this).find('.file-input').click();
        });

        // File input change
        $('.upload-zone .file-input').on('change', function () {
            var files = this.files;
            var $zone = $(this).closest('.upload-zone');
            var variantId = $zone.data('variant-id');

            if (files.length > 0) {
                uploadFiles(files, variantId, $zone);
            }

            // Reset input
            $(this).val('');
        });

        // Drag and drop
        $('.upload-zone')
            .on('dragover dragenter', function (e) {
                e.preventDefault();
                e.stopPropagation();
                $(this).addClass('dragover');
            })
            .on('dragleave drop', function (e) {
                e.preventDefault();
                e.stopPropagation();
                $(this).removeClass('dragover');
            })
            .on('drop', function (e) {
                var files = e.originalEvent.dataTransfer.files;
                var variantId = $(this).data('variant-id');

                if (files.length > 0) {
                    uploadFiles(files, variantId, $(this));
                }
            });
    }

    /**
     * Upload files to server
     */
    function uploadFiles(files, variantId, $zone) {
        var $grid = $('#images-grid-' + variantId);

        for (var i = 0; i < files.length; i++) {
            var file = files[i];

            // Validate file
            if (!validateFile(file)) {
                continue;
            }

            // Create placeholder
            var $placeholder = createUploadPlaceholder(file);
            $grid.append($placeholder);

            // Upload file
            uploadFile(file, variantId, $placeholder);
        }
    }

    /**
     * Validate file before upload
     */
    function validateFile(file) {
        // Check extension
        var ext = '.' + file.name.split('.').pop().toLowerCase();
        if (allowedExtensions.indexOf(ext) === -1) {
            showToast('Formato não permitido: ' + ext, 'error');
            return false;
        }

        // Check size
        if (file.size > maxFileSize) {
            showToast('Arquivo muito grande: ' + file.name, 'error');
            return false;
        }

        return true;
    }

    /**
     * Create upload placeholder with preview
     */
    function createUploadPlaceholder(file) {
        var $placeholder = $('<div class="image-item uploading"><img src="" alt="Enviando..." /></div>');

        // Create preview
        var reader = new FileReader();
        reader.onload = function (e) {
            $placeholder.find('img').attr('src', e.target.result);
        };
        reader.readAsDataURL(file);

        return $placeholder;
    }

    /**
     * Upload a single file
     */
    function uploadFile(file, variantId, $placeholder) {
        var formData = new FormData();
        formData.append('file', file);
        formData.append('variantId', variantId);

        // Obter token CSRF atualizado
        var token = $('input[name="__RequestVerificationToken"]').val();
        formData.append('__RequestVerificationToken', token);

        $.ajax({
            url: '/Products/UploadVariantImage',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    // Replace placeholder with actual image
                    var $item = createImageItem(response.image);
                    $placeholder.replaceWith($item);

                    // Update count
                    var $grid = $('#images-grid-' + variantId);
                    var count = $grid.find('.image-item').length;
                    updateImageCount(variantId, count);

                    showToast('Imagem enviada com sucesso', 'success');
                } else {
                    $placeholder.remove();
                    showToast(response.message || 'Erro ao enviar imagem', 'error');
                }
            },
            error: function () {
                $placeholder.remove();
                showToast('Erro ao enviar imagem', 'error');
            }
        });
    }

    /**
     * Initialize action handlers (delete, set main)
     */
    function initActionHandlers() {
        // Delete image
        $(document).on('click', '.btn-delete-image', function (e) {
            e.stopPropagation();
            var imageId = $(this).data('image-id');
            var $item = $(this).closest('.image-item');

            if (confirm('Tem certeza que deseja remover esta imagem?')) {
                deleteImage(imageId, $item);
            }
        });

        // Set main image
        $(document).on('click', '.btn-set-main', function (e) {
            e.stopPropagation();
            var imageId = $(this).data('image-id');
            var $item = $(this).closest('.image-item');

            setMainImage(imageId, $item);
        });
    }

    /**
     * Delete an image
     */
    function deleteImage(imageId, $item) {
        var $grid = $item.closest('.images-grid');
        var variantId = $item.closest('.variant-images-section').data('variant-id');

        $.ajax({
            url: '/Products/DeleteVariantImage',
            type: 'POST',
            data: { imageId: imageId },
            success: function (response) {
                if (response.success) {
                    $item.fadeOut(300, function () {
                        $(this).remove();

                        // Update count
                        var count = $grid.find('.image-item').length;
                        updateImageCount(variantId, count);

                        // If deleted was main, reload to show new main
                        if ($item.hasClass('is-main')) {
                            loadVariantImages(variantId);
                        }
                    });

                    showToast('Imagem removida', 'success');
                } else {
                    showToast(response.message || 'Erro ao remover imagem', 'error');
                }
            },
            error: function () {
                showToast('Erro ao remover imagem', 'error');
            }
        });
    }

    /**
     * Set image as main
     */
    function setMainImage(imageId, $item) {
        var variantId = $item.closest('.variant-images-section').data('variant-id');

        $.ajax({
            url: '/Products/SetMainImage',
            type: 'POST',
            data: { imageId: imageId },
            success: function (response) {
                if (response.success) {
                    // Reload images to update UI
                    loadVariantImages(variantId);
                    showToast('Imagem principal definida', 'success');
                } else {
                    showToast(response.message || 'Erro ao definir principal', 'error');
                }
            },
            error: function () {
                showToast('Erro ao definir imagem principal', 'error');
            }
        });
    }

    /**
     * Show toast notification
     */
    function showToast(message, type) {
        var $toast = $('<div class="image-toast ' + type + '">' + message + '</div>');
        $('body').append($toast);

        setTimeout(function () {
            $toast.fadeOut(300, function () {
                $(this).remove();
            });
        }, 3000);
    }

    // Initialize when document is ready
    $(document).ready(function () {
        // Only initialize if we're on a page with image sections
        if ($('.variant-images-section').length > 0) {
            init();
        }
    });

})(jQuery);
