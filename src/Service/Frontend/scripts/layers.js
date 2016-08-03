(function (FC, $) {
    (function (Layers) {
        "use strict";

        // TODO: use FC.ClientState instead of duplicating variables and categories locally
        var _variables; // list of variables returned by service
        var _categories; // list of variable categories retured by service

        var _layerSelectionPanel;

        var _$page; // layers section container

        var _$navPath; // 
        var _$navBackBtn; // return to list of variables btn

        var _$searchControl; // search input
        var _$sortControl; // sort control

        var _$tilesWrapper; // wrapper for variable and data source tiles
        var _$variablesList; // nano-scoller container for list of variables
        var _$variablesListContent; // content of variables list
        var _$datasourcesList; // nano-scroller container for data sources of variable
        var _$datasourcesListContent; // content od data sources list
        var _$sectionHeader;

        var _variableTileTemplate = "#variable-template .variable";
        var _datasourceTileTemplate = "#data-source-template .data-source";
        var _categoryContainerTempalte = "#category-template .layer-category";
        var _$selectedVariableTemplate;

        var _selectedVariable; // variable which data sources are shown
        var _savedSearchQuery; // search query at variables page before navigation to data source page        
        var _currentTilesType; // indicates current tiles type: variables | datasources
        var _currentSortMode; // indicates currently seleced sort: byName | byCategory

        var _datasourceInfoPanel; // panel with info about selected data source
        var _noCategoriesFound = false;

        /**
        * Initializes layers section.
        */
        Layers.initialize = function () {
            var config = FC.state.config;
            _$page = $("section.layers");

            _layerSelectionPanel = new FC.Controls.LayersSelectionPanel($(".layers-selection-area"));
            _layerSelectionPanel.update();

            _$navPath = _$page.find(".nav-path");
            _$navBackBtn = _$page.find(".nav-back");

            _$searchControl = _$page.find("input");
            _$sortControl = _$page.find(".control-sort");
            _$sectionHeader = _$page.find(".section-header");

            _$tilesWrapper = _$page.find(".layer-tiles");
            _$variablesList = _$page.find(".variables-list");
            _$variablesListContent = _$variablesList.find(".content");
            _$datasourcesList = _$page.find(".data-sources-list");
            _$datasourcesListContent = _$datasourcesList.find(".content");

            _$selectedVariableTemplate = $("#selected-variable-template .selected-variable");

            _datasourceInfoPanel = new FC.Controls.DataSourcePanel();
            _datasourceInfoPanel.appendTo(_$page);

            _variables = config.EnvironmentalVariables;
            _categories = config.Categories;

            // create array of selected data sources to prevent circular references between data sources and variables
            _variables = _variables.map(function (variable) {
                variable.selected = !!FC.state.variables[variable.Name];
                variable.selectedDataSources = variable.selected ?
                                               FC.state.variables[variable.Name].dataSources.slice(0) :
                                               variable.DataSources.map(mapToDataSourceId);
                return variable;
            });

            // adding Variables field for each data source,
            // adding 'No category' category for variables without categories.
            _variables = _variables.map(function (variable) {
                variable.DataSources.forEach(function (datasource) {
                    var _datasource;
                    config.DataSources.forEach(function (ds) {
                        if (ds.ID === datasource.ID) {
                            _datasource = ds;
                            return;
                        }
                    });

                    datasource.Variables = _datasource.ProvidedVariables;
                });

                if (variable.Categories.length === 0) {
                    _noCategoriesFound = true;
                    _$sectionHeader.css("display", "none");
                    variable.Categories.push("No category");

                    if ($.inArray("No category", _categories) === - 1) {
                        _categories.push("No category");
                    }
                }

                return variable;
            });

            // Build list of variable tiles
            updateSelectedList();

            $(".layers-selection-panel-controls .remove-layers-btn").click(function () {
                FC.state.clearVariables();
                _$variablesListContent.find(".variable").removeClass("selected");
                _variables.forEach(function (v) { v.selected = false; });
                updateSelectedList();
                _layerSelectionPanel.update();
            });

            _$navBackBtn.on("click", function () {
                onNavBack();
            });

            _$sortControl.on("click", function () {
                _$searchControl.val("");

                switch (_currentSortMode) {
                    case "byCategory":
                        sortByName();
                        break;
                    
                    case "byName":
                        sortByCategory();
                        break;
                }
            });

            _$searchControl.on("input", function () {
                var query = $(this).val().trim();
                onFuzzySearch(query);
            });

            $(window).on("resize", function onResize () {
                var windowHeight = $(window).height();

                var headerHeight = 0;//_$navPath.height();
                var controlsHeight = 0; //_$sortControl.parent().height();
                
                // calculate total vertical padding of section and tiles container                
                var padding = 
                    parseInt(_layerSelectionPanel.$panel.css("padding-top").replace("px", ""), 10) +
                    parseInt(_layerSelectionPanel.$panel.css("padding-bottom").replace("px", ""), 10);
                //padding += parseInt(_$variablesList.parent().parent().css("padding-top").replace("px", ""), 10) +
                //    parseInt(_$variablesList.parent().parent().css("padding-bottom").replace("px", ""), 10);

                _$tilesWrapper.height(windowHeight - headerHeight - controlsHeight);
                _$variablesList.height(windowHeight - headerHeight - controlsHeight - padding);
                _$variablesList.nanoScroller();
                _$datasourcesList.height(windowHeight - headerHeight - controlsHeight - padding);
                _$datasourcesList.nanoScroller();
            });

            _currentTilesType = "variables";
            sortByName();
            $(window).trigger("resize");
        };

        function rebuildDataSourcesForSelectedVariable($div, variable) {
            var $dsDiv = $div.find(".variable-data-sources");
            $dsDiv.empty();
            var dsCount = 0;
            for (var i = 0; i < variable.DataSources.length; i++) {
                var ds = variable.DataSources[i];
                if(variable.selectedDataSources.some(function(id) { return id == ds.ID })) {
                    $("<div>" + ds.Name + "<div>").
                        addClass("data-source-name").
                        attr("title", ds.Description).appendTo($dsDiv);
                    dsCount++;
                }
            }
            if (!dsCount)
                $("<div>" + FC.Settings.NO_DATASOURCES_SELECTED_MESSAGE + "</div>").
                    addClass("no-data-source-message").
                    appendTo($dsDiv);
        }

        function createSelectedVariableDiv(variable) {
            var $newDiv = _$selectedVariableTemplate.clone(true, true);
            $newDiv.attr("data-variable-name", variable.Name)
            $newDiv.find(".variable-name").
                attr("title", variable.Description).
                text(variable.Description);
            rebuildDataSourcesForSelectedVariable($newDiv, variable);

            $newDiv.find(".remove-layer-btn").click(function () {
                // Remove variable from client state
                FC.state.toggleVariable(
                    variable.Name,
                    variable.DataSources.map(mapToDataSourceId)
                );

                // Clear selection flag
                _variables.filter(function (v) { return v.Name == variable.Name }).forEach(function (v) { v.selected = false });

                // Remove selection from tile
                _$variablesListContent.
                    find(".variable").
                    filter(function (index, v) { return $(v).data("variable").Name == variable.Name }).toggleClass("selected");

                _layerSelectionPanel.update();
                $newDiv.remove();
            });

            $newDiv.find(".toggle-edit-btn").click(function () {
                showDataSourcesForVariable(variable);
            });

            return $newDiv;
        }

        function updateSelectedList() {
            _layerSelectionPanel.update();
            var selectedVars = _variables.filter(function (v) { return v.selected });

            // Adding missing variables & correcting existing
            selectedVars.forEach(function (variable) {
                var variableName = variable.Name;
                var found = false;
                _layerSelectionPanel.$content.children().each(function (index, div) {
                    var $div = $(div);
                    if ($div.attr("data-variable-name") == variableName) {
                        found = true;
                        rebuildDataSourcesForSelectedVariable($div, variable);
                    }
                });
                if (!found) 
                    createSelectedVariableDiv(variable).appendTo(_layerSelectionPanel.$content);
            });

            // Removing deleted variables
            _layerSelectionPanel.$content.children().each(function (index, div) {
                var $div = $(div);
                var variableName = $div.attr("data-variable-name");
                if (!selectedVars.some(function (v) { return v.Name == variableName }))
                    $div.remove();

            });

            // Update nano-scrollbars
            _layerSelectionPanel.$content.parent().nanoScroller();
        }

        var mapToDataSourceId = function (dataSource) {
            return dataSource.ID;
        };

        /**
        * Creates new variable tile.
        *
        * Returns tile as jQuery object.
        */
        function createVariableTile(variable) {
            var description = variable.Description;
            var units = variable.Units;

            var $variable = $(_variableTileTemplate).clone(true, true);
            var $variableName = $variable.find(".variable-name");
            var $variableDescription = $variable.find(".variable-description");
            var $variableSources = $variable.find(".variable-data-sources");

            $variable.data("variable", variable);
            // data("name") is used for fuzzyHighlight
            $variableName.data("name", description)
                .text(description)
                .attr("title", description);
            $variableDescription.text(units);

            $variableName.dotdotdot({
                watch: window
            });

            // TODO: update when variables will be in FC.ClientState
            if (variable.selected) {
                $variable.addClass("selected");
            }

            $variable.on("click", function () {
                // save selected state of variable
                var _variable = $(this).data("variable");
                _variables.forEach(function (variable) {
                    if (variable.Name === _variable.Name) {
                        variable.selected = !variable.selected;
                        FC.state.toggleVariable(
                            variable.Name,
                            variable.DataSources.map(mapToDataSourceId)
                        );
                        updateSelectedList();
                    }
                });

                $(this).toggleClass("selected");
            });

            // click on data sources of variable
            $variableSources.on("click", function (event) {
                event.stopPropagation();

                // update global with currently selected variable
                var _$variable = $(this).parent();
                var _variable = _$variable.data("variable");
                
                showDataSourcesForVariable(_variable);               
            });

            return $variable;
        }

        function showDataSourcesForVariable(_variable) {
            _$sectionHeader.css("display", "inline-block");
            _currentTilesType = "datasources";

            _savedSearchQuery = _$searchControl.val();
            _$searchControl.val("");


            _variables.forEach(function (variable) {
                if (variable.Description === _variable.Description) {
                    _selectedVariable = variable;
                }
            });

            var datasources = _selectedVariable.DataSources;
            // var selectedDataSources = $(this).parent().data("selected-datasources") || [];

            for (var j = 0, length = datasources.length; j < length; j++) {
                createDataSourceTile(datasources[j]).appendTo(_$datasourcesListContent);
            }

            _$variablesList.hide();
            _$datasourcesList.show();
            _$navBackBtn.show();
            _$sortControl.hide();

            _$navPath.text(_variable.Description + " > Sources");
        }

        /**
        * Creates new data source tile.
        *
        * Returns tile as jQuery object.
        */
        function createDataSourceTile(datasource) {
            var name = datasource.Name;
            var description = datasource.Description;

            var $datasource = $(_datasourceTileTemplate).clone(true, true);
            var $datasourceName = $datasource.find(".data-source-name");
            var $datasourceDescription = $datasource.find(".data-source-description");
            var $datasourceInfo = $datasource.find(".data-source-info");

            $datasourceName.text(name)
                .attr("title", name);
            $datasourceDescription.text(description);
            $datasource.data("datasource", datasource);

            // TODO: update when variables will be in FC.ClientState
            if (-1 !== _selectedVariable.selectedDataSources.indexOf(datasource.ID)) {
                $datasource.addClass("selected");
            }

            $datasourceDescription.dotdotdot({
                watch: window
            });

            $datasource.on("click", function () {
                var _datasource = $(this).data("datasource");

                // add or remove datasource from variable selected datasources
                // TODO: find a better solution
                var index;
                if (-1 !== (index = _selectedVariable.selectedDataSources.indexOf(_datasource.ID))) {
                    _selectedVariable.selectedDataSources.splice(index, 1);
                }
                else {
                    _selectedVariable.selectedDataSources.push(_datasource.ID);
                }

                if (_selectedVariable.selected) {
                    FC.state.toggleDataSource(_selectedVariable.Name, _datasource.ID);
                    updateSelectedList();
                }

                $(this).toggleClass("selected");
            });

            $datasourceInfo.on("click", function (event) {
                event.stopPropagation();
                
                _datasourceInfoPanel.initialize(datasource);
                _datasourceInfoPanel.show();
                // TODO: show panel with data source info
            });

            return $datasource;
        }

        /**
        * Sorts variables by category.
        */
        function sortByCategory() {
            _$variablesListContent.empty();
            _currentSortMode = "byCategory";
            _$navPath.text(FC.Settings.LAYERS_BY_CATEGORY_MESSAGE);
            _$sortControl.attr("title", "Sort by name");

            _categories.forEach(function (category) {
                // create category container
                var $categoryContainer = $(_categoryContainerTempalte).clone(true, true);
                var $categoryName = $categoryContainer.find(".category-name");
                $categoryName.text(category);

                // add variables to category
                var $categoryContent = $categoryContainer.find(".category-content");
                _variables.sort(function (a, b) {
                    if(a.Description < b.Description) return -1;
                    if(a.Description > b.Description) return 1;
                    return 0;
                }).forEach(function (variable) {
                    if (-1 === variable.Categories.indexOf(category)) {
                        return false;
                    }

                    createVariableTile(variable).appendTo($categoryContent);
                });

                $categoryContainer.appendTo(_$variablesListContent);
            });

            _$variablesList.nanoScroller();
        }

        /**
        * Sort variables by name.
        */
        function sortByName() {
            _$variablesListContent.empty();
            _currentSortMode = "byName";
            _$navPath.text(FC.Settings.LAYERS_BY_NAME_MESSAGE);
            _$sortControl.attr("title", "Sort by category");

            _variables.sort(function (a, b) {
                if(a.Description < b.Description) return -1;
                if(a.Description > b.Description) return 1;
                return 0;
            }).forEach(function (variable) {
                createVariableTile(variable).appendTo(_$variablesListContent);
            });

            _$variablesList.nanoScroller();
        }

        /**
        * Fuzzy searches variables or datasources by given search query.
        */
        function onFuzzySearch(query) {
            var searchSuccess = false;

            // TODO: rewrite this code, make more general code on this function.
            // Add container where to search as a parameter.
            if ("variables" === _currentTilesType) {
                _$variablesListContent.empty();

                _variables.sort(function (a, b) {
                    if(a.Description < b.Description) return -1;
                    if(a.Description > b.Description) return 1;
                    return 0;
                }).forEach(function (variable) {
                    var name = variable.Description;
                    var pattern = new RegExp(query, "gi");
                    if (pattern.test(name)) {
                        searchSuccess = true;
                        createVariableTile(variable).appendTo(_$variablesListContent)
                            .find(".variable-name")
                            .highlightSubstring(query);
                    }
                });

                if (false === searchSuccess) {
                    _$variablesListContent.text("No results found.");
                }
                else {
                    _$variablesList.nanoScroller();
                }
            }
            else {
                _$datasourcesListContent.empty();

                _selectedVariable.DataSources.sort(function (a, b) {
                    if(a.Name < b.Name) return -1;
                    if(a.Name > b.Name) return 1;
                    return 0;
                }).forEach(function (datasource) {
                    var name = datasource.Name;
                    var pattern = new RegExp(query, "gi");
                    if (pattern.test(name)) {
                        searchSuccess = true;
                        // data("name") is used for fuzzyHighlight
                        createDataSourceTile(datasource).appendTo(_$datasourcesListContent)
                            .find(".data-source-name")
                            .data("name", datasource.Name)
                            .highlightSubstring(query);
                    }
                });

                if (false === searchSuccess) {
                    _$datasourcesListContent.text("No results found.");
                }
                else {
                    _$variablesList.nanoScroller();
                }
            }

            return searchSuccess;
        }

        /**
        * Returns from data sources list to list of variables.
        */
        function onNavBack() {
            _currentTilesType = "variables";
            _selectedVariable = null;
            _$searchControl.val(_savedSearchQuery);

            _datasourceInfoPanel.hide();
            _$datasourcesList.hide();
            _$datasourcesListContent.empty();
            _$variablesList.show();
            _$navBackBtn.hide();
            _$sortControl.show();

            _$navPath.text(_currentSortMode == "byName" ?
                FC.Settings.LAYERS_BY_NAME_MESSAGE : FC.Settings.LAYERS_BY_CATEGORY_MESSAGE);

            if(_noCategoriesFound)
                _$sectionHeader.css("display", "none");
        }

    })(FC.Layers || (FC.Layers = {}));
})(window.FC = window.FC || {}, jQuery);