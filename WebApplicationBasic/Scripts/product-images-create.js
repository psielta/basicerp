/**
 * Product Images Management for Create Wizard
 * Handles image selection, preview, and form submission
 * Version: 1.0
 */

(function ($) {
    'use strict';

    // Store selected files per variant
    var selectedFiles = {};
    var mainImageIndex = {};

    // Allowed extensions and max size
    var allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
    var maxFileSize = 5 * 1024 * 1024; // 5MB

    /**
     * Initialize image management
     */
    function init() {
        initUploadZones();
        initActionHandlers();

        // Hook into variant generation for configurable products
        $(document).on('variantsGenerated', onVariantsGenerated);
    }

    /**
     * Initialize upload zones
     */
    function initUploadZones() {
        // Click to open file dialog
        $(document).on('click', '.create-upload-zone', function (e) {
            if ($(e.target).is('input')) return;
            $(this).find('.create-file-input').click();
        });

        // File input change
        $(document).on('change', '.create-file-input', function () {
            var files = this.files;
            var $zone = $(this).closest('.create-upload-zone');
            var variantIndex = $zone.data('variant-index');

            if (files.length > 0) {
                addFiles(files, variantIndex);
            }
        });

        // Drag and drop
        $(document).on('dragover dragenter', '.create-upload-zone', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).addClass('dragover');
        });

        $(document).on('dragleave drop', '.create-upload-zone', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).removeClass('dragover');
        });

        $(document).on('drop', '.create-upload-zone', function (e) {
            var files = e.originalEvent.dataTransfer.files;
            var variantIndex = $(this).data('variant-index');

            if (files.length > 0) {
                addFiles(files, variantIndex);
            }
        });
    }

    /**
     * Add files to selection
     */
    function addFiles(files, variantIndex) {
        if (!selectedFiles[variantIndex]) {
            selectedFiles[variantIndex] = [];
            mainImageIndex[variantIndex] = 0;
        }

        for (var i = 0; i < files.length; i++) {
            var file = files[i];

            if (validateFile(file)) {
                selectedFiles[variantIndex].push(file);
            }
        }

        // Set first as main if this is the first batch
        if (selectedFiles[variantIndex].length > 0 && mainImageIndex[variantIndex] === undefined) {
            mainImageIndex[variantIndex] = 0;
        }

        renderPreviews(variantIndex);
        updateFileInputs(variantIndex);
    }

    /**
     * Validate file
     */
    function validateFile(file) {
        var ext = '.' + file.name.split('.').pop().toLowerCase();

        if (allowedExtensions.indexOf(ext) === -1) {
            alert('Formato não permitido: ' + ext + '\nUse: ' + allowedExtensions.join(', '));
            return false;
        }

        if (file.size > maxFileSize) {
            alert('Arquivo muito grande: ' + file.name + '\nMáximo: 5MB');
            return false;
        }

        return true;
    }

    /**
     * Render image previews
     */
    function renderPreviews(variantIndex) {
        var $grid = getPreviewGrid(variantIndex);
        $grid.empty();

        var files = selectedFiles[variantIndex] || [];
        var mainIdx = mainImageIndex[variantIndex] || 0;

        files.forEach(function (file, index) {
            var isMain = index === mainIdx;
            var $item = createPreviewItem(file, variantIndex, index, isMain);
            $grid.append($item);
        });

        // Update count
        updateImageCount(variantIndex, files.length);
    }

    /**
     * Get preview grid for variant
     */
    function getPreviewGrid(variantIndex) {
        if (variantIndex === 'simple') {
            return $('#simple-images-preview');
        } else {
            return $('#variant-' + variantIndex + '-images-preview');
        }
    }

    /**
     * Create preview item
     */
    function createPreviewItem(file, variantIndex, index, isMain) {
        var $item = $('<div class="image-preview-item' + (isMain ? ' is-main' : '') + '" data-index="' + index + '"></div>');

        if (isMain) {
            $item.append('<span class="main-badge">Principal</span>');
        }

        var $img = $('<img src="" alt="Preview" />');
        $item.append($img);

        // Read file for preview
        var reader = new FileReader();
        reader.onload = function (e) {
            $img.attr('src', e.target.result);
        };
        reader.readAsDataURL(file);

        // Overlay with actions
        var $overlay = $('<div class="preview-overlay"></div>');
        var $actions = $('<div class="preview-actions"></div>');

        if (!isMain) {
            $actions.append('<button type="button" class="btn-preview-main" data-variant="' + variantIndex + '" data-index="' + index + '">Principal</button>');
        }
        $actions.append('<button type="button" class="btn-preview-remove" data-variant="' + variantIndex + '" data-index="' + index + '">Remover</button>');

        $overlay.append($actions);
        $item.append($overlay);

        return $item;
    }

    /**
     * Update image count badge
     */
    function updateImageCount(variantIndex, count) {
        var $section;
        if (variantIndex === 'simple') {
            $section = $('.create-images-section[data-variant-index="simple"]');
        } else {
            $section = $('.variant-image-section[data-variant-index="' + variantIndex + '"]');
        }

        var $badge = $section.find('.images-count');
        $badge.text(count + ' ' + (count === 1 ? 'imagem' : 'imagens'));
    }

    /**
     * Update hidden file inputs for form submission
     */
    function updateFileInputs(variantIndex) {
        var $section;
        if (variantIndex === 'simple') {
            $section = $('.create-images-section[data-variant-index="simple"]');
        } else {
            $section = $('.variant-image-section[data-variant-index="' + variantIndex + '"]');
        }

        // Remove old hidden inputs
        $section.find('input.image-file-input').remove();

        var files = selectedFiles[variantIndex] || [];
        var mainIdx = mainImageIndex[variantIndex] || 0;

        // Create a DataTransfer to hold files
        var dt = new DataTransfer();

        // Add main image first, then others
        if (files[mainIdx]) {
            dt.items.add(files[mainIdx]);
        }
        files.forEach(function (file, idx) {
            if (idx !== mainIdx) {
                dt.items.add(file);
            }
        });

        // Update file input
        var $fileInput = $section.find('.create-file-input');
        $fileInput[0].files = dt.files;

        // Store main image index for backend
        var inputName = variantIndex === 'simple' ? 'MainImageIndex' : 'VariantMainImageIndex[' + variantIndex + ']';
        $section.find('input[name="' + inputName + '"]').remove();
        // Main is always 0 since we reorder
        $section.append('<input type="hidden" name="' + inputName + '" value="0" class="image-file-input" />');
    }

    /**
     * Initialize action handlers
     */
    function initActionHandlers() {
        // Remove image
        $(document).on('click', '.btn-preview-remove', function (e) {
            e.stopPropagation();
            var variantIndex = $(this).data('variant');
            var index = $(this).data('index');

            removeImage(variantIndex, index);
        });

        // Set main image
        $(document).on('click', '.btn-preview-main', function (e) {
            e.stopPropagation();
            var variantIndex = $(this).data('variant');
            var index = $(this).data('index');

            setMainImage(variantIndex, index);
        });
    }

    /**
     * Remove image from selection
     */
    function removeImage(variantIndex, index) {
        var files = selectedFiles[variantIndex];
        if (!files) return;

        files.splice(index, 1);

        // Adjust main index if needed
        if (mainImageIndex[variantIndex] >= files.length) {
            mainImageIndex[variantIndex] = Math.max(0, files.length - 1);
        } else if (mainImageIndex[variantIndex] > index) {
            mainImageIndex[variantIndex]--;
        }

        renderPreviews(variantIndex);
        updateFileInputs(variantIndex);
    }

    /**
     * Set main image
     */
    function setMainImage(variantIndex, index) {
        mainImageIndex[variantIndex] = index;
        renderPreviews(variantIndex);
        updateFileInputs(variantIndex);
    }

    /**
     * Called when variants are generated (for configurable products)
     */
    function onVariantsGenerated(event, variants) {
        var $container = $('#variant-images-container');
        $('#no-variants-images-msg').hide();

        // Clear existing sections (keep the message)
        $container.find('.variant-image-section').remove();

        // Reset stored files for old variants
        selectedFiles = {};
        mainImageIndex = {};

        // Create section for each variant
        variants.forEach(function (variant, index) {
            var html = createVariantImageSection(variant, index);
            $container.append(html);
        });
    }

    /**
     * Create image section for a variant
     */
    function createVariantImageSection(variant, index) {
        var description = variant.description || 'Variante ' + (index + 1);
        var sku = variant.sku || '';

        var html = '<div class="variant-image-section card mb-3" data-variant-index="' + index + '">' +
            '<div class="card-header d-flex justify-content-between align-items-center">' +
            '<div><strong>' + description + '</strong>' +
            (sku ? '<span class="badge bg-primary ms-2">SKU: ' + sku + '</span>' : '') +
            '</div>' +
            '<span class="badge bg-secondary images-count">0 imagens</span>' +
            '</div>' +
            '<div class="card-body">' +
            '<div class="images-preview-grid" id="variant-' + index + '-images-preview"></div>' +
            '<div class="create-upload-zone" data-variant-index="' + index + '">' +
            '<input type="file" name="VariantImages[' + index + ']" class="create-file-input" accept="image/*" multiple style="display: none;" />' +
            '<div class="upload-content">' +
            '<i class="bi bi-cloud-upload" style="font-size: 2rem; color: #6c757d;"></i>' +
            '<p class="mb-0 mt-2">Arraste imagens ou clique para selecionar</p>' +
            '<small class="text-muted">JPG, PNG, GIF, WebP - Máximo 5MB por imagem</small>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '</div>';

        return html;
    }

    /**
     * Get all selected files (for form validation)
     */
    window.getSelectedImageFiles = function () {
        return selectedFiles;
    };

    // Initialize when document is ready
    $(document).ready(function () {
        if ($('.create-upload-zone').length > 0 || $('#variant-images-container').length > 0) {
            init();
        }
    });

})(jQuery);
