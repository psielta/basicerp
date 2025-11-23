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
        var productType = $('#ProductType').val();

        // Add specific validations per tab
        switch (currentTab) {
            case 0: // Basic Info
                var name = $('#Name').val();
                if (!name || name.trim() === '') {
                    alert('Por favor, informe o nome do produto.');
                    valid = false;
                }
                break;
            case 2: // SKU (for simple products) or Variant Attributes (for configurable)
                if (productType === '0') { // Simple product - tab 2 is SKU
                    var sku = $('#Sku').val();
                    if (!sku || sku.trim() === '') {
                        alert('Por favor, informe o SKU do produto.');
                        valid = false;
                    }
                }
                // For configurable products, tab 2 is attributes (no validation needed here)
                break;
            case 3: // Variations (for configurable products only)
                if (productType === '1') { // Configurable product - validate variants exist
                    var variantCount = $('#variants-tbody tr.variant-row').length;
                    if (variantCount === 0) {
                        alert('Por favor, gere pelo menos uma variação.');
                        valid = false;
                    }
                }
                // For simple products, tab 3 is Review (no validation needed)
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

    // Toggle variant details (delegated event for dynamically created elements)
    $(document).on('click', '.toggle-details', function (e) {
        e.stopPropagation();
        var targetId = $(this).data('target');
        var detailsRow = $(targetId);

        if (detailsRow.is(':visible')) {
            detailsRow.hide();
            $(this).removeClass('expanded');
        } else {
            detailsRow.show();
            $(this).addClass('expanded');
        }
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

            // LINHA PRINCIPAL (variant-row)
            var mainRow = $('<tr class="variant-row" data-variant-index="' + index + '">');

            // Coluna 1: Botão expandir
            mainRow.append(
                '<td>' +
                '<button type="button" class="btn btn-sm btn-outline-secondary toggle-details" data-target="#variant-details-' + index + '">' +
                '<i class="bi bi-chevron-right"></i>' +
                '</button>' +
                '</td>'
            );

            // Coluna 2: SKU
            mainRow.append(
                '<td>' +
                '<input type="text" name="Variants[' + index + '].Sku" class="form-control form-control-sm" value="' + sku + '" required />' +
                '</td>'
            );

            // Coluna 3: Atributos (exibição + hidden fields)
            var attrCell = '<td><strong>' + description + '</strong>';
            combo.forEach(function (attr, attrIndex) {
                attrCell += '<input type="hidden" name="Variants[' + index + '].VariantAttributeValuesList[' + attrIndex + '].AttributeId" value="' + attr.attributeId + '" />';
                attrCell += '<input type="hidden" name="Variants[' + index + '].VariantAttributeValuesList[' + attrIndex + '].AttributeValueId" value="' + attr.valueId + '" />';
            });
            attrCell += '</td>';
            mainRow.append(attrCell);

            // Coluna 4: Custo
            mainRow.append(
                '<td>' +
                '<input type="text" name="Variants[' + index + '].Cost" class="form-control form-control-sm" placeholder="0.00" />' +
                '</td>'
            );

            // Coluna 5: Ativo
            mainRow.append(
                '<td class="text-center">' +
                '<input type="checkbox" name="Variants[' + index + '].IsActive" value="true" class="form-check-input" checked />' +
                '<input type="hidden" name="Variants[' + index + '].IsActive" value="false" />' +
                '</td>'
            );

            tbody.append(mainRow);

            // LINHA EXPANSÍVEL (variant-details-row)
            var detailsRow = $('<tr class="variant-details-row" id="variant-details-' + index + '" style="display: none;">');

            var detailsContent =
                '<td></td>' + // Célula vazia alinhada com botão
                '<td colspan="4">' +
                '<div class="card">' +
                '<div class="card-body">' +
                '<div class="row">' +

                // Coluna Esquerda
                '<div class="col-md-6">' +
                '<div class="mb-3">' +
                '<label class="form-label">Nome da Variante</label>' +
                '<input type="text" name="Variants[' + index + '].Name" class="form-control form-control-sm" value="' + description + '" placeholder="Opcional" />' +
                '</div>' +
                '<div class="mb-3">' +
                '<label class="form-label">Descrição</label>' +
                '<textarea name="Variants[' + index + '].Description" class="form-control form-control-sm" rows="2" placeholder="Opcional">' + description + '</textarea>' +
                '</div>' +
                '<div class="mb-3">' +
                '<label class="form-label">Código de Barras</label>' +
                '<input type="text" name="Variants[' + index + '].Barcode" class="form-control form-control-sm" placeholder="EAN/GTIN" />' +
                '</div>' +
                '</div>' +

                // Coluna Direita: Dimensões
                '<div class="col-md-6">' +
                '<h6 class="mb-3">Dimensões e Peso</h6>' +
                '<div class="row">' +
                '<div class="col-md-6">' +
                '<div class="mb-3">' +
                '<label class="form-label">Peso</label>' +
                '<div class="input-group input-group-sm">' +
                '<input type="text" name="Variants[' + index + '].Weight" class="form-control" placeholder="0.000" />' +
                '<span class="input-group-text">kg</span>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="mb-3">' +
                '<label class="form-label">Altura</label>' +
                '<div class="input-group input-group-sm">' +
                '<input type="text" name="Variants[' + index + '].Height" class="form-control" placeholder="cm" />' +
                '<span class="input-group-text">cm</span>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="row">' +
                '<div class="col-md-6">' +
                '<div class="mb-3">' +
                '<label class="form-label">Largura</label>' +
                '<div class="input-group input-group-sm">' +
                '<input type="text" name="Variants[' + index + '].Width" class="form-control" placeholder="cm" />' +
                '<span class="input-group-text">cm</span>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="mb-3">' +
                '<label class="form-label">Comprimento</label>' +
                '<div class="input-group input-group-sm">' +
                '<input type="text" name="Variants[' + index + '].Length" class="form-control" placeholder="cm" />' +
                '<span class="input-group-text">cm</span>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '</div>' +

                '</div>' + // Fecha row
                '</div>' + // Fecha card-body
                '</div>' + // Fecha card
                '</td>';

            detailsRow.append(detailsContent);
            tbody.append(detailsRow);
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
            var variantCount = $('#variants-tbody tr.variant-row').length;
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
