// Product Edit Wizard JavaScript
$(document).ready(function () {
    var currentTab = 0;
    var tabs = $('.nav-pills li');
    var tabPanes = $('.tab-pane');
    var newVariantCounter = 0;
    var totalAttributes = $('.attribute-selection-group').length;

    // Coletar combinações existentes das variantes já salvas
    var existingCombinations = collectExistingCombinations();

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

                    // Check new variants (adicionados via modal)
                    if (!hasActiveVariant) {
                        $('#new-variants-container input[name*="Variants"][name$=".IsActive"]').each(function () {
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

    // ========================================
    // MODAL DE ADICIONAR NOVA VARIAÇÃO
    // ========================================

    // Quando um radio button é selecionado no modal
    $('.new-variant-attr-radio').on('change', function () {
        updateModalPreview();
        validateModalCombination();
        updateAddButtonState();
    });

    // Validar SKU no modal
    $('#newVariantSku').on('input', function () {
        validateModalSku();
        updateAddButtonState();
    });

    // Reset modal quando abrir
    $('#addVariantModal').on('show.bs.modal', function () {
        resetModal();
    });

    // Botão de adicionar variação
    $('#btnAddNewVariant').on('click', function () {
        addNewVariantFromModal();
    });

    function resetModal() {
        // Limpar seleções de atributos
        $('.new-variant-attr-radio').prop('checked', false);

        // Limpar campos
        $('#newVariantSku').val('').removeClass('is-valid is-invalid');
        $('#newVariantSkuFeedback').text('').hide();
        $('#newVariantCost').val('');
        $('#newVariantName').val('');
        $('#newVariantBarcode').val('');
        $('#newVariantWeight').val('');
        $('#newVariantHeight').val('');
        $('#newVariantWidth').val('');
        $('#newVariantLength').val('');
        $('#newVariantIsActive').prop('checked', true);

        // Resetar preview e status
        $('#modal-variant-preview').html('Selecione os atributos acima');
        $('#modal-combination-status').hide().removeClass('alert-success alert-danger alert-info');

        // Desabilitar botão
        $('#btnAddNewVariant').prop('disabled', true);

        // Gerar SKU sugerido
        var skuBase = $('#Name').val() ? $('#Name').val().substring(0, 3).toUpperCase() : 'PRD';
        var existingSkus = collectAllExistingSkus();
        var suggestedSku = generateUniqueSku(skuBase, existingSkus);
        $('#newVariantSku').val(suggestedSku);
        validateModalSku();
    }

    function updateModalPreview() {
        var selectedAttributes = [];

        $('.attribute-selection-group').each(function () {
            var $checked = $(this).find('.new-variant-attr-radio:checked');
            if ($checked.length > 0) {
                var attrName = $checked.data('attribute-name');
                var valueName = $checked.data('value-name');
                selectedAttributes.push(attrName + ': ' + valueName);
            }
        });

        if (selectedAttributes.length > 0) {
            $('#modal-variant-preview').html('<strong>' + selectedAttributes.join(', ') + '</strong>');
        } else {
            $('#modal-variant-preview').html('Selecione os atributos acima');
        }
    }

    function validateModalCombination() {
        var selectedValues = [];

        $('.attribute-selection-group').each(function () {
            var $checked = $(this).find('.new-variant-attr-radio:checked');
            if ($checked.length > 0) {
                selectedValues.push($checked.val());
            }
        });

        // Se não selecionou todos os atributos, não valida ainda
        if (selectedValues.length < totalAttributes) {
            $('#modal-combination-status').hide();
            return { valid: true, complete: false };
        }

        // Ordenar para comparação
        selectedValues.sort();
        var combinationJson = JSON.stringify(selectedValues);

        // Verificar se já existe (nas variantes existentes ou nas novas adicionadas)
        var allCombinations = existingCombinations.concat(collectNewVariantCombinations());
        var exists = allCombinations.indexOf(combinationJson) !== -1;

        if (exists) {
            $('#modal-combination-status')
                .removeClass('alert-info alert-success')
                .addClass('alert-danger')
                .html('<i class="bi bi-exclamation-triangle"></i> <strong>Esta combinação já existe!</strong> Escolha valores diferentes.')
                .show();
            return { valid: false, complete: true };
        } else {
            $('#modal-combination-status')
                .removeClass('alert-info alert-danger')
                .addClass('alert-success')
                .html('<i class="bi bi-check-circle"></i> <strong>Combinação válida!</strong> Esta variação pode ser criada.')
                .show();
            return { valid: true, complete: true };
        }
    }

    function validateModalSku() {
        var sku = $('#newVariantSku').val();
        var $input = $('#newVariantSku');
        var $feedback = $('#newVariantSkuFeedback');

        if (!sku || sku.trim() === '') {
            $input.removeClass('is-invalid is-valid');
            $feedback.hide();
            return false;
        }

        var skuUpper = sku.trim().toUpperCase();
        var existingSkus = collectAllExistingSkus().map(function (s) { return s.toUpperCase(); });

        if (existingSkus.indexOf(skuUpper) !== -1) {
            $input.addClass('is-invalid').removeClass('is-valid');
            $feedback.text('Este SKU já existe.').show();
            return false;
        } else {
            $input.addClass('is-valid').removeClass('is-invalid');
            $feedback.hide();
            return true;
        }
    }

    function updateAddButtonState() {
        var combinationResult = validateModalCombination();
        var skuValid = validateModalSku();

        // Verificar se todos os atributos foram selecionados
        var allAttributesSelected = true;
        $('.attribute-selection-group').each(function () {
            if ($(this).find('.new-variant-attr-radio:checked').length === 0) {
                allAttributesSelected = false;
                return false;
            }
        });

        var canAdd = combinationResult.valid && combinationResult.complete && skuValid && allAttributesSelected;
        $('#btnAddNewVariant').prop('disabled', !canAdd);
    }

    function addNewVariantFromModal() {
        // Coletar dados do modal
        var selectedAttrs = [];
        var description = [];

        $('.attribute-selection-group').each(function () {
            var $checked = $(this).find('.new-variant-attr-radio:checked');
            if ($checked.length > 0) {
                selectedAttrs.push({
                    attributeId: $checked.data('attribute-id'),
                    attributeName: $checked.data('attribute-name'),
                    valueId: $checked.val(),
                    valueName: $checked.data('value-name')
                });
                description.push($checked.data('attribute-name') + ': ' + $checked.data('value-name'));
            }
        });

        var sku = $('#newVariantSku').val().trim();
        var cost = $('#newVariantCost').val();
        var name = $('#newVariantName').val() || description.join(', ');
        var barcode = $('#newVariantBarcode').val();
        var weight = $('#newVariantWeight').val();
        var height = $('#newVariantHeight').val();
        var width = $('#newVariantWidth').val();
        var length = $('#newVariantLength').val();
        var isActive = $('#newVariantIsActive').is(':checked');
        var descriptionText = description.join(', ');

        // Calcular índice da nova variante
        var existingVariantCount = $('#existing-variants table tbody tr.variant-row').length;
        var newVariantCount = $('#new-variants-container table tbody tr.variant-row').length;
        var variantIndex = existingVariantCount + newVariantCount;

        // Se não existe tabela de novas variantes, criar
        if ($('#new-variants-container table').length === 0) {
            var tableHtml = '<hr /><h4><i class="bi bi-plus-circle"></i> Novas Variações a Adicionar</h4>';
            tableHtml += '<p class="text-muted small"><i class="bi bi-info-circle"></i> Clique no botão <i class="bi bi-chevron-right"></i> para expandir e editar detalhes.</p>';
            tableHtml += '<table class="table table-hover variant-table">';
            tableHtml += '<thead><tr>';
            tableHtml += '<th style="width: 40px;"></th>';
            tableHtml += '<th style="width: 180px;">SKU</th>';
            tableHtml += '<th>Atributos</th>';
            tableHtml += '<th style="width: 120px;">Custo (R$)</th>';
            tableHtml += '<th style="width: 80px;" class="text-center">Ativo</th>';
            tableHtml += '<th style="width: 60px;"></th>';
            tableHtml += '</tr></thead>';
            tableHtml += '<tbody></tbody></table>';
            $('#new-variants-container').html(tableHtml);
        }

        var detailsId = 'new-variant-details-' + variantIndex;

        // Criar linha da nova variante
        var rowHtml = '<tr class="variant-row" data-variant-index="' + variantIndex + '">';

        // Botão expandir
        rowHtml += '<td>';
        rowHtml += '<button type="button" class="btn btn-sm btn-outline-secondary toggle-details-new" data-target="#' + detailsId + '">';
        rowHtml += '<i class="bi bi-chevron-right"></i>';
        rowHtml += '</button>';
        rowHtml += '</td>';

        // SKU
        rowHtml += '<td>';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Sku" class="form-control form-control-sm" value="' + escapeHtml(sku) + '" required />';
        rowHtml += '</td>';

        // Atributos
        rowHtml += '<td><strong>' + escapeHtml(descriptionText) + '</strong>';
        selectedAttrs.forEach(function (attr, attrIndex) {
            rowHtml += '<input type="hidden" name="Variants[' + variantIndex + '].VariantAttributeValuesList[' + attrIndex + '].AttributeId" value="' + attr.attributeId + '" />';
            rowHtml += '<input type="hidden" name="Variants[' + variantIndex + '].VariantAttributeValuesList[' + attrIndex + '].AttributeValueId" value="' + attr.valueId + '" />';
        });
        rowHtml += '</td>';

        // Custo
        rowHtml += '<td>';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Cost" class="form-control form-control-sm" value="' + escapeHtml(cost) + '" placeholder="0.00" />';
        rowHtml += '</td>';

        // Ativo
        rowHtml += '<td class="text-center">';
        rowHtml += '<input type="checkbox" name="Variants[' + variantIndex + '].IsActive" value="true" class="form-check-input" ' + (isActive ? 'checked' : '') + ' />';
        rowHtml += '<input type="hidden" name="Variants[' + variantIndex + '].IsActive" value="false" />';
        rowHtml += '</td>';

        // Botão remover
        rowHtml += '<td>';
        rowHtml += '<button type="button" class="btn btn-sm btn-outline-danger remove-new-variant" data-index="' + variantIndex + '" title="Remover">';
        rowHtml += '<i class="bi bi-trash"></i>';
        rowHtml += '</button>';
        rowHtml += '</td>';

        rowHtml += '</tr>';

        // Linha de detalhes expansível
        rowHtml += '<tr class="variant-details-row" id="' + detailsId + '" style="display: none;">';
        rowHtml += '<td></td>';
        rowHtml += '<td colspan="5">';
        rowHtml += '<div class="card"><div class="card-body"><div class="row">';

        // Coluna esquerda
        rowHtml += '<div class="col-md-6">';
        rowHtml += '<div class="mb-3"><label class="form-label">Nome da Variante</label>';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Name" class="form-control form-control-sm" value="' + escapeHtml(name) + '" placeholder="Opcional" /></div>';
        rowHtml += '<div class="mb-3"><label class="form-label">Descrição</label>';
        rowHtml += '<textarea name="Variants[' + variantIndex + '].Description" class="form-control form-control-sm" rows="2" placeholder="Opcional">' + escapeHtml(descriptionText) + '</textarea></div>';
        rowHtml += '<div class="mb-3"><label class="form-label">Código de Barras</label>';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Barcode" class="form-control form-control-sm" value="' + escapeHtml(barcode) + '" placeholder="EAN/GTIN" /></div>';
        rowHtml += '</div>';

        // Coluna direita (dimensões)
        rowHtml += '<div class="col-md-6">';
        rowHtml += '<h6 class="mb-3">Dimensões e Peso</h6>';
        rowHtml += '<div class="row">';
        rowHtml += '<div class="col-md-6"><div class="mb-3"><label class="form-label">Peso</label><div class="input-group input-group-sm">';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Weight" class="form-control" value="' + escapeHtml(weight) + '" placeholder="0.000" /><span class="input-group-text">kg</span></div></div></div>';
        rowHtml += '<div class="col-md-6"><div class="mb-3"><label class="form-label">Altura</label><div class="input-group input-group-sm">';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Height" class="form-control" value="' + escapeHtml(height) + '" placeholder="cm" /><span class="input-group-text">cm</span></div></div></div>';
        rowHtml += '</div><div class="row">';
        rowHtml += '<div class="col-md-6"><div class="mb-3"><label class="form-label">Largura</label><div class="input-group input-group-sm">';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Width" class="form-control" value="' + escapeHtml(width) + '" placeholder="cm" /><span class="input-group-text">cm</span></div></div></div>';
        rowHtml += '<div class="col-md-6"><div class="mb-3"><label class="form-label">Comprimento</label><div class="input-group input-group-sm">';
        rowHtml += '<input type="text" name="Variants[' + variantIndex + '].Length" class="form-control" value="' + escapeHtml(length) + '" placeholder="cm" /><span class="input-group-text">cm</span></div></div></div>';
        rowHtml += '</div></div>';

        rowHtml += '</div></div></div>';
        rowHtml += '</td></tr>';

        // Adicionar à tabela
        $('#new-variants-container table tbody').append(rowHtml);

        // Atualizar lista de combinações existentes
        existingCombinations = collectExistingCombinations();

        // Fechar modal
        $('#addVariantModal').modal('hide');

        // Scroll para a nova variante
        $('html, body').animate({
            scrollTop: $('#new-variants-container').offset().top - 100
        }, 300);

        newVariantCounter++;
    }

    // Função para coletar combinações das variantes existentes (do servidor)
    function collectExistingCombinations() {
        var combinations = [];

        $('#existing-variants table tbody tr.variant-row').each(function () {
            var combo = [];
            $(this).find('input[name*="VariantAttributeValuesList"][name$=".AttributeValueId"]').each(function () {
                combo.push($(this).val());
            });
            if (combo.length > 0) {
                combo.sort();
                combinations.push(JSON.stringify(combo));
            }
        });

        return combinations;
    }

    // Função para coletar combinações das novas variantes (adicionadas via modal)
    function collectNewVariantCombinations() {
        var combinations = [];

        $('#new-variants-container table tbody tr.variant-row').each(function () {
            var combo = [];
            $(this).find('input[name*="VariantAttributeValuesList"][name$=".AttributeValueId"]').each(function () {
                combo.push($(this).val());
            });
            if (combo.length > 0) {
                combo.sort();
                combinations.push(JSON.stringify(combo));
            }
        });

        return combinations;
    }

    // Eventos delegados para elementos dinâmicos
    $(document).on('click', '.toggle-details-new', function (e) {
        e.stopPropagation();
        var targetId = $(this).data('target');
        var detailsRow = $(targetId);

        if (detailsRow.is(':visible')) {
            detailsRow.hide();
            $(this).removeClass('expanded').find('i').removeClass('bi-chevron-down').addClass('bi-chevron-right');
        } else {
            detailsRow.show();
            $(this).addClass('expanded').find('i').removeClass('bi-chevron-right').addClass('bi-chevron-down');
        }
    });

    $(document).on('click', '.remove-new-variant', function () {
        var $btn = $(this);
        var index = $btn.data('index');

        if (confirm('Tem certeza que deseja remover esta variação?')) {
            // Remover linha principal e linha de detalhes
            $btn.closest('tr.variant-row').next('tr.variant-details-row').remove();
            $btn.closest('tr.variant-row').remove();

            // Se não há mais variantes novas, remover a tabela
            if ($('#new-variants-container table tbody tr').length === 0) {
                $('#new-variants-container').empty();
            }

            // Atualizar lista de combinações
            existingCombinations = collectExistingCombinations();
        }
    });

    // Helper para escape de HTML
    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
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

        // SKUs das novas variantes adicionadas via modal
        $('#new-variants-container input[name*="Variants"][name$=".Sku"]').each(function () {
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

            // SKUs novos (adicionados via modal)
            $('#new-variants-container input[name*="Variants"][name$=".Sku"]').each(function () {
                var sku = $(this).val();
                if (sku && sku.trim() !== '') allSkus.push(sku);
            });

            var existingVariantCount = $('#existing-variants table tbody tr.variant-row').length;
            var newVariantCount = $('#new-variants-container table tbody tr.variant-row').length;
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