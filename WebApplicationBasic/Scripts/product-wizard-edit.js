// Product Edit Wizard JavaScript
$(document).ready(function () {
    var currentTab = 0;
    var tabs = $('.nav-pills li');
    var tabPanes = $('.tab-pane');
    var newVariantCounter = 0;

    // Initialize wizard
    showTab(currentTab);
    updateSelectedCategories();

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
                var productType = $('#ProductType').val();
                if (productType === '0') { // Simple product
                    var sku = $('#Sku').val();
                    if (!sku || sku.trim() === '') {
                        alert('Por favor, informe o SKU do produto.');
                        valid = false;
                    }
                } else { // Configurable product
                    // Check if at least one variant is active
                    var hasActiveVariant = false;
                    $('input[name*=".IsActive"]').each(function () {
                        if ($(this).is(':checked')) {
                            hasActiveVariant = true;
                            return false;
                        }
                    });
                    if (!hasActiveVariant) {
                        alert('O produto deve ter pelo menos uma variante ativa.');
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

    // Variant attributes toggle (for new attributes only)
    $('.attribute-toggle:not(:disabled)').change(function () {
        var attributeId = $(this).data('attribute-id');
        var panel = $(this).closest('.variant-attribute-panel');
        var valuesDiv = panel.find('.attribute-values');

        if ($(this).is(':checked')) {
            panel.addClass('active');
            valuesDiv.slideDown();
        } else {
            panel.removeClass('active');
            valuesDiv.slideUp();
            valuesDiv.find('input[type="checkbox"]:not(:disabled)').prop('checked', false);
        }
    });

    // Generate new variants button
    $('#generate-new-variants').click(function () {
        generateNewVariants();
    });

    // Generate new variants function
    function generateNewVariants() {
        var selectedAttributes = [];

        // Collect only new attribute selections (not disabled)
        $('.variant-attribute-panel.active').each(function () {
            var attributeId = $(this).data('attribute-id');
            var attributeName = $(this).find('.attribute-toggle').siblings('strong').text().replace('Em uso', '').trim();
            var newValues = [];

            $(this).find('.attribute-value-checkbox:checked:not(:disabled)').each(function () {
                newValues.push({
                    id: $(this).val(),
                    name: $(this).data('value-name')
                });
            });

            if (newValues.length > 0) {
                selectedAttributes.push({
                    id: attributeId,
                    name: attributeName,
                    values: newValues
                });
            }
        });

        if (selectedAttributes.length === 0) {
            alert('Selecione pelo menos um novo valor de atributo para gerar variações adicionais.');
            return;
        }

        // Generate all combinations
        var combinations = cartesianProduct(selectedAttributes);

        if (combinations.length === 0) {
            return;
        }

        // Display new variants
        $('#new-variants-container').show();
        var tableHtml = '<table class="table table-striped table-bordered">';
        tableHtml += '<thead><tr><th>SKU</th><th>Atributos</th><th>Custo</th><th>Peso</th><th>Código de Barras</th><th>Ativo</th></tr></thead>';
        tableHtml += '<tbody>';

        var existingVariantCount = $('#existing-variants table tbody tr').length;
        var skuBase = $('#Name').val() ? $('#Name').val().substring(0, 3).toUpperCase() : 'PRD';

        combinations.forEach(function (combo, index) {
            var variantIndex = existingVariantCount + newVariantCounter + index;
            var sku = skuBase + '-NEW-' + (newVariantCounter + index + 1).toString().padStart(3, '0');
            var description = combo.map(function (c) { return c.attributeName + ': ' + c.valueName; }).join(', ');

            tableHtml += '<tr>';
            tableHtml += '<td><input type="text" name="Variants[' + variantIndex + '].Sku" class="form-control input-sm" value="' + sku + '" /></td>';
            tableHtml += '<td>' + description;

            // Add hidden fields for variant attributes
            combo.forEach(function (attr) {
                tableHtml += '<input type="hidden" name="Variants[' + variantIndex + '].VariantAttributeValues[' + attr.attributeId + ']" value="' + attr.valueId + '" />';
            });

            tableHtml += '</td>';
            tableHtml += '<td><input type="number" name="Variants[' + variantIndex + '].Cost" class="form-control input-sm" step="0.01" min="0" /></td>';
            tableHtml += '<td><input type="number" name="Variants[' + variantIndex + '].Weight" class="form-control input-sm" step="0.001" min="0" /></td>';
            tableHtml += '<td><input type="text" name="Variants[' + variantIndex + '].Barcode" class="form-control input-sm" /></td>';
            tableHtml += '<td><input type="checkbox" name="Variants[' + variantIndex + '].IsActive" value="true" checked /><input type="hidden" name="Variants[' + variantIndex + '].IsActive" value="false" /></td>';
            tableHtml += '</tr>';
        });

        tableHtml += '</tbody></table>';
        $('#new-variants-table').html(tableHtml);

        newVariantCounter += combinations.length;
        alert('Foram geradas ' + combinations.length + ' novas variações. Revise antes de salvar.');
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

        var productType = $('#ProductType').val();
        summary += '<dt>Tipo:</dt><dd>' + (productType === '0' ? 'Produto Simples' : 'Produto com Variações') + '</dd>';

        // Categories
        var selectedCategories = [];
        $('#category-tree input:checked').each(function () {
            selectedCategories.push($(this).parent().text().trim());
        });
        summary += '<dt>Categorias:</dt><dd>' + (selectedCategories.length > 0 ? selectedCategories.join(', ') : 'Nenhuma') + '</dd>';

        // Variants count
        if (productType === '1') { // Configurable
            var existingVariantCount = $('#existing-variants table tbody tr').length;
            var newVariantCount = $('#new-variants-table tbody tr').length;
            summary += '<dt>Variantes Existentes:</dt><dd>' + existingVariantCount + '</dd>';
            if (newVariantCount > 0) {
                summary += '<dt>Novas Variantes:</dt><dd>' + newVariantCount + '</dd>';
            }
        } else {
            summary += '<dt>SKU:</dt><dd>' + ($('#Sku').val() || '-') + '</dd>';
        }

        // Changes indicator
        summary += '<dt>Status:</dt><dd><span class="label label-warning">Alterações pendentes</span></dd>';

        summary += '</dl>';
        $('#product-summary').html(summary);
    }

    // Form submission
    $('#editProductForm').submit(function (e) {
        if (!validateUpToTab(tabs.length - 1)) {
            e.preventDefault();
            alert('Por favor, complete todos os campos obrigatórios.');
            return false;
        }
    });
});