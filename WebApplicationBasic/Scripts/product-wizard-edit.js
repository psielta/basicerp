// Product Edit Wizard JavaScript
$(document).ready(function () {
    var currentTab = 0;
    var tabs = $('.nav-pills li');
    var tabPanes = $('.tab-pane');
    var newVariantCounter = 0;

    // Initialize wizard
    showTab(currentTab);
    updateSelectedCategories();

    // Toggle variant details
    $('.toggle-details').click(function (e) {
        e.stopPropagation();
        var targetId = $(this).data('target');
        var detailsRow = $(targetId);
        var icon = $(this).find('i');

        if (detailsRow.is(':visible')) {
            detailsRow.hide();
            $(this).removeClass('expanded');
        } else {
            detailsRow.show();
            $(this).addClass('expanded');
        }
    });

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
            case 3: // Variations (for configurable products) or Review (for simple products)
                if (productType === '1') { // Configurable product - tab 3 is Variations
                    // Check if at least one variant is active (existing or new)
                    var hasActiveVariant = false;

                    // Check existing variants
                    $('#existing-variants input[name*="Variants"][name$=".IsActive"]').each(function () {
                        if ($(this).is(':checked')) {
                            hasActiveVariant = true;
                            return false;
                        }
                    });

                    // Check new variants if no active existing variant found
                    if (!hasActiveVariant) {
                        $('#new-variants-table input[name*="Variants"][name$=".IsActive"]').each(function () {
                            if ($(this).is(':checked')) {
                                hasActiveVariant = true;
                                return false;
                            }
                        });
                    }

                    if (!hasActiveVariant) {
                        alert('O produto deve ter pelo menos uma variante ativa.');
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
        // Coletar atributos em uso (valores disabled/checked) e novos valores selecionados
        var attributesInUse = [];
        var newAttributeValues = [];

        $('.variant-attribute-panel.active').each(function () {
            var attributeId = $(this).data('attribute-id');
            var attributeName = $(this).find('.attribute-toggle').siblings('strong').text().replace('Em uso', '').trim();

            // Valores já em uso (disabled e checked)
            var existingValues = [];
            $(this).find('.attribute-value-checkbox:checked:disabled').each(function () {
                existingValues.push({
                    id: $(this).val(),
                    name: $(this).data('value-name')
                });
            });

            // Novos valores selecionados (checked mas não disabled)
            var newValues = [];
            $(this).find('.attribute-value-checkbox:checked:not(:disabled)').each(function () {
                newValues.push({
                    id: $(this).val(),
                    name: $(this).data('value-name')
                });
            });

            if (existingValues.length > 0 || newValues.length > 0) {
                attributesInUse.push({
                    id: attributeId,
                    name: attributeName,
                    existingValues: existingValues,
                    newValues: newValues,
                    allValues: existingValues.concat(newValues)
                });
            }

            if (newValues.length > 0) {
                newAttributeValues.push({
                    id: attributeId,
                    name: attributeName,
                    values: newValues
                });
            }
        });

        if (newAttributeValues.length === 0) {
            alert('Selecione pelo menos um novo valor de atributo para gerar variações adicionais.');
            return;
        }

        // Gerar combinações que incluam pelo menos um novo valor
        // Estratégia: gerar todas as combinações possíveis e filtrar apenas as que contêm novos valores
        var allCombinations = generateAllCombinations(attributesInUse);

        // Filtrar apenas combinações que contêm pelo menos um valor novo
        var newCombinations = allCombinations.filter(function(combo) {
            return combo.some(function(item) {
                return item.isNew;
            });
        });

        if (newCombinations.length === 0) {
            alert('Não há novas combinações para gerar. Verifique se selecionou novos valores de atributos.');
            return;
        }

        // Usar newCombinations como combinations para o resto da função
        var combinations = newCombinations;

        // Contar variantes existentes (apenas linhas principais, não as de detalhes)
        var existingVariantCount = $('#existing-variants table tbody tr.variant-row').length;
        var skuBase = $('#Name').val() ? $('#Name').val().substring(0, 3).toUpperCase() : 'PRD';

        // Coletar todos os SKUs existentes para evitar duplicidade
        var existingSkus = collectAllExistingSkus();

        // Display new variants com estrutura expansível
        $('#new-variants-container').show();

        var tableHtml = '<p class="text-muted small">';
        tableHtml += '<i class="bi bi-info-circle"></i> ';
        tableHtml += 'Clique no botão <i class="bi bi-chevron-right"></i> para expandir e editar todos os detalhes da variante.';
        tableHtml += '</p>';
        tableHtml += '<table class="table table-hover variant-table">';
        tableHtml += '<thead><tr>';
        tableHtml += '<th style="width: 40px;"></th>';
        tableHtml += '<th style="width: 180px;">SKU</th>';
        tableHtml += '<th>Atributos</th>';
        tableHtml += '<th style="width: 120px;">Custo (R$)</th>';
        tableHtml += '<th style="width: 80px;" class="text-center">Ativo</th>';
        tableHtml += '</tr></thead>';
        tableHtml += '<tbody>';

        combinations.forEach(function (combo, index) {
            var variantIndex = existingVariantCount + newVariantCounter + index;
            // Gerar SKU único que não exista ainda
            var sku = generateUniqueSku(skuBase, existingSkus);
            existingSkus.push(sku.toUpperCase()); // Adicionar ao array para evitar duplicidade nas próximas iterações
            var description = combo.map(function (c) { return c.attributeName + ': ' + c.valueName; }).join(', ');
            var detailsId = 'new-variant-details-' + variantIndex;

            // LINHA PRINCIPAL (variant-row)
            tableHtml += '<tr class="variant-row" data-variant-index="' + variantIndex + '">';

            // Coluna 1: Botão expandir
            tableHtml += '<td>';
            tableHtml += '<button type="button" class="btn btn-sm btn-outline-secondary toggle-details" data-target="#' + detailsId + '">';
            tableHtml += '<i class="bi bi-chevron-right"></i>';
            tableHtml += '</button>';
            tableHtml += '</td>';

            // Coluna 2: SKU
            tableHtml += '<td>';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Sku" class="form-control form-control-sm" value="' + sku + '" required />';
            tableHtml += '</td>';

            // Coluna 3: Atributos (exibição + hidden fields)
            tableHtml += '<td><strong>' + description + '</strong>';
            combo.forEach(function (attr, attrIndex) {
                tableHtml += '<input type="hidden" name="Variants[' + variantIndex + '].VariantAttributeValuesList[' + attrIndex + '].AttributeId" value="' + attr.attributeId + '" />';
                tableHtml += '<input type="hidden" name="Variants[' + variantIndex + '].VariantAttributeValuesList[' + attrIndex + '].AttributeValueId" value="' + attr.valueId + '" />';
            });
            tableHtml += '</td>';

            // Coluna 4: Custo
            tableHtml += '<td>';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Cost" class="form-control form-control-sm" placeholder="0.00" />';
            tableHtml += '</td>';

            // Coluna 5: Ativo
            tableHtml += '<td class="text-center">';
            tableHtml += '<input type="checkbox" name="Variants[' + variantIndex + '].IsActive" value="true" class="form-check-input" checked />';
            tableHtml += '<input type="hidden" name="Variants[' + variantIndex + '].IsActive" value="false" />';
            tableHtml += '</td>';

            tableHtml += '</tr>';

            // LINHA EXPANSÍVEL (variant-details-row)
            tableHtml += '<tr class="variant-details-row" id="' + detailsId + '" style="display: none;">';
            tableHtml += '<td></td>';
            tableHtml += '<td colspan="4">';
            tableHtml += '<div class="card">';
            tableHtml += '<div class="card-body">';
            tableHtml += '<div class="row">';

            // Coluna Esquerda
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Nome da Variante</label>';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Name" class="form-control form-control-sm" value="' + description + '" placeholder="Opcional" />';
            tableHtml += '</div>';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Descrição</label>';
            tableHtml += '<textarea name="Variants[' + variantIndex + '].Description" class="form-control form-control-sm" rows="2" placeholder="Opcional">' + description + '</textarea>';
            tableHtml += '</div>';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Código de Barras</label>';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Barcode" class="form-control form-control-sm" placeholder="EAN/GTIN" />';
            tableHtml += '</div>';
            tableHtml += '</div>';

            // Coluna Direita: Dimensões
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<h6 class="mb-3">Dimensões e Peso</h6>';
            tableHtml += '<div class="row">';
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Peso</label>';
            tableHtml += '<div class="input-group input-group-sm">';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Weight" class="form-control" placeholder="0.000" />';
            tableHtml += '<span class="input-group-text">kg</span>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Altura</label>';
            tableHtml += '<div class="input-group input-group-sm">';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Height" class="form-control" placeholder="cm" />';
            tableHtml += '<span class="input-group-text">cm</span>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '<div class="row">';
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Largura</label>';
            tableHtml += '<div class="input-group input-group-sm">';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Width" class="form-control" placeholder="cm" />';
            tableHtml += '<span class="input-group-text">cm</span>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '<div class="col-md-6">';
            tableHtml += '<div class="mb-3">';
            tableHtml += '<label class="form-label">Comprimento</label>';
            tableHtml += '<div class="input-group input-group-sm">';
            tableHtml += '<input type="text" name="Variants[' + variantIndex + '].Length" class="form-control" placeholder="cm" />';
            tableHtml += '<span class="input-group-text">cm</span>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';
            tableHtml += '</div>';

            tableHtml += '</div>'; // Fecha row
            tableHtml += '</div>'; // Fecha card-body
            tableHtml += '</div>'; // Fecha card
            tableHtml += '</td>';
            tableHtml += '</tr>';
        });

        tableHtml += '</tbody></table>';
        $('#new-variants-table').html(tableHtml);

        // Bind eventos de toggle para as novas variantes
        $('#new-variants-table .toggle-details').click(function (e) {
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

        newVariantCounter += combinations.length;
        alert('Foram geradas ' + combinations.length + ' novas variações. Revise antes de salvar.');
    }

    // Gera todas as combinações possíveis marcando quais valores são novos
    function generateAllCombinations(attributes) {
        if (attributes.length === 0) return [];
        if (attributes.length === 1) {
            return attributes[0].allValues.map(function (v) {
                var isNew = attributes[0].newValues.some(function(nv) { return nv.id === v.id; });
                return [{
                    attributeId: attributes[0].id,
                    attributeName: attributes[0].name,
                    valueId: v.id,
                    valueName: v.name,
                    isNew: isNew
                }];
            });
        }

        var result = [];
        var restCombinations = generateAllCombinations(attributes.slice(1));
        attributes[0].allValues.forEach(function (v) {
            var isNew = attributes[0].newValues.some(function(nv) { return nv.id === v.id; });
            restCombinations.forEach(function (p) {
                result.push([{
                    attributeId: attributes[0].id,
                    attributeName: attributes[0].name,
                    valueId: v.id,
                    valueName: v.name,
                    isNew: isNew
                }].concat(p));
            });
        });
        return result;
    }

    // Coleta todos os SKUs existentes (variantes existentes + novas já geradas)
    function collectAllExistingSkus() {
        var skus = [];

        // SKUs das variantes existentes
        $('#existing-variants input[name*="Variants"][name$=".Sku"]').each(function () {
            var sku = $(this).val();
            if (sku && sku.trim() !== '') {
                skus.push(sku.trim().toUpperCase());
            }
        });

        // SKUs das novas variantes já geradas nesta sessão
        $('#new-variants-table input[name*="Variants"][name$=".Sku"]').each(function () {
            var sku = $(this).val();
            if (sku && sku.trim() !== '') {
                skus.push(sku.trim().toUpperCase());
            }
        });

        return skus;
    }

    // Gera um SKU único que não exista na lista de SKUs existentes
    function generateUniqueSku(base, existingSkus) {
        var counter = 1;
        var maxAttempts = 9999; // Evitar loop infinito
        var sku;

        do {
            sku = base + '-NEW-' + counter.toString().padStart(3, '0');
            counter++;
        } while (existingSkus.indexOf(sku.toUpperCase()) !== -1 && counter <= maxAttempts);

        // Se esgotou as tentativas com padrão NEW, usar timestamp
        if (counter > maxAttempts) {
            sku = base + '-' + Date.now().toString(36).toUpperCase();
        }

        return sku;
    }

    // Cartesian product for combinations (mantido para compatibilidade)
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

        // Variants/SKU
        if (productType === '1') { // Configurable
            // Coletar todos os SKUs (existentes e novos)
            var allSkus = [];

            // SKUs existentes - busca inputs cujo name contém "Variants" e termina com ".Sku"
            $('#existing-variants input[name*="Variants"][name$=".Sku"]').each(function () {
                var sku = $(this).val();
                if (sku && sku.trim() !== '') allSkus.push(sku);
            });

            // SKUs novos
            $('#new-variants-table input[name*="Variants"][name$=".Sku"]').each(function () {
                var sku = $(this).val();
                if (sku && sku.trim() !== '') allSkus.push(sku);
            });

            var existingVariantCount = $('#existing-variants table tbody tr.variant-row').length;
            var newVariantCount = $('#new-variants-table tbody tr.variant-row').length;
            var totalVariants = existingVariantCount + newVariantCount;

            summary += '<dt>Total de Variantes:</dt><dd>' + totalVariants + '</dd>';

            if (existingVariantCount > 0) {
                summary += '<dt>Variantes Existentes:</dt><dd>' + existingVariantCount + '</dd>';
            }
            if (newVariantCount > 0) {
                summary += '<dt>Novas Variantes:</dt><dd>' + newVariantCount + '</dd>';
            }

            // Mostrar lista de SKUs
            if (allSkus.length > 0) {
                summary += '<dt>SKUs:</dt><dd>';
                if (allSkus.length <= 10) {
                    summary += '<ul class="list-unstyled mb-0">';
                    allSkus.forEach(function (sku) {
                        summary += '<li><code>' + sku + '</code></li>';
                    });
                    summary += '</ul>';
                } else {
                    // Se muitos SKUs, mostrar resumido
                    summary += '<ul class="list-unstyled mb-0">';
                    for (var i = 0; i < 5; i++) {
                        summary += '<li><code>' + allSkus[i] + '</code></li>';
                    }
                    summary += '<li>... e mais ' + (allSkus.length - 5) + ' SKU(s)</li>';
                    summary += '</ul>';
                }
                summary += '</dd>';
            }
        } else {
            summary += '<dt>SKU:</dt><dd><code>' + ($('#Sku').val() || '-') + '</code></dd>';
        }

        // Changes indicator
        summary += '<dt>Status:</dt><dd><span class="badge bg-warning text-dark">Alterações pendentes</span></dd>';

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

        // Validar SKUs duplicados antes de enviar
        var duplicateSkuValidation = validateDuplicateSkus();
        if (!duplicateSkuValidation.valid) {
            e.preventDefault();
            alert('Existem SKUs duplicados no formulário:\n\n' + duplicateSkuValidation.duplicates.join('\n') + '\n\nPor favor, altere para valores únicos.');
            highlightDuplicateSkus(duplicateSkuValidation.duplicates);
            return false;
        }
    });

    // Validar SKUs duplicados no formulário
    function validateDuplicateSkus() {
        var skus = [];
        var duplicates = [];

        // Coletar todos os SKUs (existentes e novos)
        $('input[name*="Variants"][name$=".Sku"]').each(function () {
            var sku = $(this).val();
            if (sku && sku.trim() !== '') {
                skus.push({
                    sku: sku.trim().toUpperCase(),
                    originalSku: sku.trim(),
                    element: $(this)
                });
            }
        });

        // Encontrar duplicados
        var skuCounts = {};
        skus.forEach(function (item) {
            if (skuCounts[item.sku]) {
                skuCounts[item.sku]++;
            } else {
                skuCounts[item.sku] = 1;
            }
        });

        for (var sku in skuCounts) {
            if (skuCounts[sku] > 1) {
                // Encontrar o SKU original (com case original)
                var originalSku = skus.find(function (s) { return s.sku === sku; }).originalSku;
                duplicates.push(originalSku);
            }
        }

        return {
            valid: duplicates.length === 0,
            duplicates: duplicates
        };
    }

    // Destacar inputs com SKUs duplicados
    function highlightDuplicateSkus(duplicates) {
        // Remover highlights anteriores
        $('input[name*="Variants"][name$=".Sku"]').removeClass('is-invalid');

        // Adicionar highlight nos duplicados
        $('input[name*="Variants"][name$=".Sku"]').each(function () {
            var sku = $(this).val();
            if (sku && sku.trim() !== '') {
                var skuUpper = sku.trim().toUpperCase();
                var isDuplicate = duplicates.some(function (d) {
                    return d.toUpperCase() === skuUpper;
                });
                if (isDuplicate) {
                    $(this).addClass('is-invalid');
                }
            }
        });
    }

    // Validar SKU em tempo real ao sair do campo
    $(document).on('blur', 'input[name*="Variants"][name$=".Sku"]', function () {
        var currentInput = $(this);
        var currentSku = currentInput.val();

        if (!currentSku || currentSku.trim() === '') {
            currentInput.removeClass('is-invalid');
            return;
        }

        var currentSkuUpper = currentSku.trim().toUpperCase();
        var isDuplicate = false;

        // Verificar se existe outro campo com o mesmo SKU
        $('input[name*="Variants"][name$=".Sku"]').not(currentInput).each(function () {
            var otherSku = $(this).val();
            if (otherSku && otherSku.trim().toUpperCase() === currentSkuUpper) {
                isDuplicate = true;
                return false; // break do each
            }
        });

        if (isDuplicate) {
            currentInput.addClass('is-invalid');
            // Mostrar tooltip ou mensagem
            if (!currentInput.next('.invalid-feedback').length) {
                currentInput.after('<div class="invalid-feedback">SKU duplicado</div>');
            }
        } else {
            currentInput.removeClass('is-invalid');
            currentInput.next('.invalid-feedback').remove();
        }
    });
});