// Product Wizard JavaScript
$(document).ready(function () {
    var currentTab = 0;
    var tabs = $('.nav-pills li');
    var tabPanes = $('.tab-pane');

    // Initialize wizard
    showTab(currentTab);

    // Next button click
    $('#nextBtn').click(function () {
        if (validateCurrentTab()) {
            currentTab++;
            showTab(currentTab);
        }
    });

    // Previous button click
    $('#prevBtn').click(function () {
        currentTab--;
        showTab(currentTab);
    });

    // Tab click
    tabs.find('a').click(function (e) {
        e.preventDefault();
        var clickedIndex = $(this).parent().index();
        if (clickedIndex <= currentTab || validateUpToTab(clickedIndex - 1)) {
            currentTab = clickedIndex;
            showTab(currentTab);
        }
    });

    // Show/hide tab
    function showTab(n) {
        // Hide all tabs
        tabPanes.removeClass('active');
        tabs.removeClass('active');

        // Show current tab
        $(tabPanes[n]).addClass('active');
        $(tabs[n]).addClass('active');

        // Mark completed tabs
        for (var i = 0; i < n; i++) {
            $(tabs[i]).addClass('completed');
        }

        // Update buttons
        if (n === 0) {
            $('#prevBtn').hide();
        } else {
            $('#prevBtn').show();
        }

        if (n === (tabs.length - 1)) {
            $('#nextBtn').hide();
            $('#submitBtn').show();
            generateSummary();
        } else {
            $('#nextBtn').show();
            $('#submitBtn').hide();
        }
    }

    // Validate current tab
    function validateCurrentTab() {
        var valid = true;
        var currentPane = $(tabPanes[currentTab]);

        // Add specific validations per tab
        switch (currentTab) {
            case 0: // Basic Info
                var name = $('#Name').val();
                if (!name || name.trim() === '') {
                    alert('Por favor, informe o nome do produto.');
                    valid = false;
                }
                break;
            case 3: // SKU/Variations
                if ($('#ProductType').val() === '0') { // Simple product
                    var sku = $('#Sku').val();
                    if (!sku || sku.trim() === '') {
                        alert('Por favor, informe o SKU do produto.');
                        valid = false;
                    }
                }
                break;
        }

        return valid;
    }

    // Validate up to a specific tab
    function validateUpToTab(tabIndex) {
        for (var i = 0; i <= tabIndex; i++) {
            currentTab = i;
            if (!validateCurrentTab()) {
                return false;
            }
        }
        return true;
    }

    // Category selection
    $('#category-tree input[type="checkbox"]').change(function () {
        updateSelectedCategories();
    });

    function updateSelectedCategories() {
        var selected = [];
        $('#category-tree input:checked').each(function () {
            var categoryName = $(this).parent().text().trim();
            selected.push(categoryName);
        });

        if (selected.length > 0) {
            $('#selected-categories').html('<ul>' + selected.map(function (cat) {
                return '<li>' + cat + '</li>';
            }).join('') + '</ul>');
        } else {
            $('#selected-categories').html('<p class="text-muted">Nenhuma categoria selecionada</p>');
        }
    }

    // Variant attributes toggle
    $('.attribute-toggle').change(function () {
        var attributeId = $(this).data('attribute-id');
        var panel = $(this).closest('.variant-attribute-panel');
        var valuesDiv = panel.find('.attribute-values');

        if ($(this).is(':checked')) {
            panel.addClass('active');
            valuesDiv.slideDown();
        } else {
            panel.removeClass('active');
            valuesDiv.slideUp();
            valuesDiv.find('input[type="checkbox"]').prop('checked', false);
        }
    });

    // Generate variants button
    $('#generate-variants').click(function () {
        generateVariants();
    });

    // Generate variants function
    function generateVariants() {
        var selectedAttributes = [];

        // Collect selected attributes and their values
        $('.variant-attribute-panel.active').each(function () {
            var attributeId = $(this).data('attribute-id');
            var attributeName = $(this).find('.attribute-toggle').siblings('strong').text();
            var values = [];

            $(this).find('.attribute-value-checkbox:checked').each(function () {
                values.push({
                    id: $(this).val(),
                    name: $(this).data('value-name')
                });
            });

            if (values.length > 0) {
                selectedAttributes.push({
                    id: attributeId,
                    name: attributeName,
                    values: values
                });
            }
        });

        if (selectedAttributes.length === 0) {
            $('#variant-preview').html('<p class="text-warning">Selecione pelo menos um atributo de variação com valores.</p>');
            $('#variants-table').hide();
            return;
        }

        // Generate all combinations
        var combinations = cartesianProduct(selectedAttributes);

        // Display preview
        $('#variant-preview').html('<p class="text-success">Serão criadas <strong>' + combinations.length + '</strong> variações:</p>');

        // Create table rows
        var tbody = $('#variants-tbody');
        tbody.empty();

        combinations.forEach(function (combo, index) {
            var skuBase = $('#Name').val() ? $('#Name').val().substring(0, 3).toUpperCase() : 'PRD';
            var sku = skuBase + '-' + (index + 1).toString().padStart(3, '0');
            var description = combo.map(function (c) { return c.attributeName + ': ' + c.valueName; }).join(', ');

            var row = $('<tr>');
            row.append('<td><input type="text" name="Variants[' + index + '].Sku" class="form-control" value="' + sku + '" /></td>');
            row.append('<td>' + description + '</td>');
            row.append('<td><input type="number" name="Variants[' + index + '].Cost" class="form-control" step="0.01" min="0" /></td>');
            row.append('<td><input type="number" name="Variants[' + index + '].Weight" class="form-control" step="0.001" min="0" /></td>');
            row.append('<td><input type="text" name="Variants[' + index + '].Barcode" class="form-control" /></td>');
            row.append('<td><input type="checkbox" name="Variants[' + index + '].IsActive" value="true" checked /><input type="hidden" name="Variants[' + index + '].IsActive" value="false" /></td>');

            // Add hidden fields for variant attributes
            combo.forEach(function (attr) {
                row.append('<input type="hidden" name="Variants[' + index + '].VariantAttributeValues[' + attr.attributeId + ']" value="' + attr.valueId + '" />');
            });

            tbody.append(row);
        });

        $('#variants-table').show();
    }

    // Cartesian product for combinations
    function cartesianProduct(attributes) {
        if (attributes.length === 0) return [];
        if (attributes.length === 1) {
            return attributes[0].values.map(function (v) {
                return [{
                    attributeId: attributes[0].id,
                    attributeName: attributes[0].name,
                    valueId: v.id,
                    valueName: v.name
                }];
            });
        }

        var result = [];
        var restProduct = cartesianProduct(attributes.slice(1));
        attributes[0].values.forEach(function (v) {
            restProduct.forEach(function (p) {
                result.push([{
                    attributeId: attributes[0].id,
                    attributeName: attributes[0].name,
                    valueId: v.id,
                    valueName: v.name
                }].concat(p));
            });
        });
        return result;
    }

    // Generate summary
    function generateSummary() {
        var summary = '<dl class="dl-horizontal">';

        // Basic info
        summary += '<dt>Nome:</dt><dd>' + ($('#Name').val() || '-') + '</dd>';
        summary += '<dt>Marca:</dt><dd>' + ($('#Brand').val() || '-') + '</dd>';
        summary += '<dt>Tipo:</dt><dd>' + ($('#ProductType').val() === '0' ? 'Produto Simples' : 'Produto com Variações') + '</dd>';

        // Categories
        var selectedCategories = [];
        $('#category-tree input:checked').each(function () {
            selectedCategories.push($(this).parent().text().trim());
        });
        summary += '<dt>Categorias:</dt><dd>' + (selectedCategories.length > 0 ? selectedCategories.join(', ') : 'Nenhuma') + '</dd>';

        // Attributes
        var selectedAttributes = [];
        $('.attribute-toggle:checked').each(function () {
            selectedAttributes.push($(this).siblings('strong').text());
        });
        if (selectedAttributes.length > 0) {
            summary += '<dt>Atributos de Variação:</dt><dd>' + selectedAttributes.join(', ') + '</dd>';
        }

        // Variants count
        if ($('#ProductType').val() === '1') { // Configurable
            var variantCount = $('#variants-tbody tr').length;
            summary += '<dt>Número de Variações:</dt><dd>' + variantCount + '</dd>';
        } else {
            summary += '<dt>SKU:</dt><dd>' + ($('#Sku').val() || '-') + '</dd>';
        }

        summary += '</dl>';
        $('#product-summary').html(summary);
    }

    // Form submission
    $('#productForm').submit(function (e) {
        if (!validateUpToTab(tabs.length - 1)) {
            e.preventDefault();
            alert('Por favor, complete todos os campos obrigatórios.');
            return false;
        }
    });
});