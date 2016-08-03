(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.Panel = function (source) {
            var self = this;

            var _$panel = typeof source !== "undefined" ? source : $("<div></div>");
            _$panel.addClass("fc-panel");

            Object.defineProperty(self, "$panel", {
                get: function () {
                    return _$panel;
                }
            });

            self.append = function ($child) {
                _$panel.append($child);
            };

            self.appendTo = function ($parent) {
                _$panel.appendTo($parent);
            };

            self.show = function (animation) {
                _$panel.show.apply(_$panel, animation);
            };

            self.hide = function (animation) {
                _$panel.hide.apply(_$panel, animation);
            };
        };

        Controls.Panel.isAnyRightPanelVisible = function () {
            return $(".fc-panel.right").is(":visible");
        };

        Controls.NamedPanel = function (source) {
            var self = this;

            self.base = Controls.Panel;
            self.base(source);

            var _$header;
            var _$content;

            Object.defineProperties(self, {
                $header: {
                    get: function () {
                        return _$header;
                    },
                    set: function (title) {
                        _$header.text(title);
                    }
                },
                $content: {
                    get: function () {
                        return _$content;
                    }
                }
            });

            _$header = typeof source !== "undefined" ? self.$panel.find(".fc-panel-header") :
                $("<div></div", {
                    class: "fc-panel-header"
                }).appendTo(self.$panel);
            _$content = typeof source !== "undefined" ? self.$panel.find(".fc-panel-content") :
                $("<div></div>", {
                    class: "fc-panel-content"
                }).appendTo(self.$panel);
        };
        Controls.NamedPanel.prototype = new Controls.Panel();

        Controls.DataSourcePanel = function (source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            self.$panel.addClass("datasource-panel right");

            var _$description;
            var _$details;

            Object.defineProperties(self, {
                $description: {
                    get: function () {
                        return _$description;
                    },
                    set: function (description) {
                        _$description.text(description);
                    }
                },
                $details: {
                    get: function () {
                        return _$details;
                    },
                    set: function (details) {
                        if ("undefined" === typeof details) {
                            return false;
                        }

                        var _variables = $("<div></div>");
                        $("<strong></strong>", {
                            text: "Variables: "
                        }).appendTo(_variables);
                        $("<span></span>", {
                            text: details.ProvidedVariables.join(", ")
                        }).appendTo(_variables);

                        var _copyright = $("<div></div>");
                        $("<strong></strong>", {
                            text: "Copyright: "
                        }).appendTo(_copyright);
                        $("<span></span>", {
                            text: details.Copyright || "Not provided"
                        }).appendTo(_copyright);

                        _$details.append(_variables)
                            .append(_copyright);
                    }
                }
            });

            // Hide the panel on click outside of the panel.
            function onDocumentClick(event) {
                if (!self.$panel.is(event.target) && !self.$panel.has(event.target).length) {
                    self.hide();
                    $(document).off("click", onDocumentClick);
                }
            }

            self.initialize = function (datasource) {
                self.$content.empty();

                _$description = $("<div></div>", {
                    class: "datasource-description",
                }).appendTo(self.$content);
                _$details = $("<div></div>", {
                    class: "datasource-details"
                }).appendTo(self.$content);

                self.$header = datasource.Name;
                self.$description = datasource.Description;
                self.$details = datasource;

                $(document).click(onDocumentClick);
            };

            var _baseHide = self.hide;
            self.hide = function () {
                _baseHide(["slide",
                    { direction: "right" },
                    500]);
            };

            var _baseShow = self.show;
            self.show = function () {
                _baseShow(["slide",
                    { direction: "right" },
                    500]);
            };
        };
        Controls.DataSourcePanel.prototype = new Controls.NamedPanel();

        Controls.MapOptionsPanel = function (source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            // TODO: Show attribution of the chosen map, limit map types for
            // problematic markets.
            var _mapTypes = {
                // worldTopo: {
                //     title: "World Topography",
                //     value: D3.BingMaps.ESRI.GetWorldTopo()
                // },
                // deLorme: {
                //     title: "DeLorme World Basemap",
                //     value: D3.BingMaps.ESRI.GetDeLorme()
                // },
                // worldImagery: {
                //     title: "ESRI World Imager",
                //     value: D3.BingMaps.ESRI.GetWorldImagery()
                // },
                // oceanBasemap: {
                //     title: "Ocean Basemap",
                //     value: D3.BingMaps.ESRI.GetOceanBasemap()
                // },
                // worldTerrainBase: {
                //     title: "World Terrain Base",
                //     value: D3.BingMaps.ESRI.GetWorldTerrainBase()
                // },
                // nationalGeographicMap: {
                //     title: "National Geographic World Map",
                //     value: D3.BingMaps.ESRI.GetNationalGeographicMap()
                // },
                // worldShadedRelief: {
                //     title: "World Shaded Relief",
                //     value: D3.BingMaps.ESRI.GetWorldShadedRelief()
                // },
                // tileSource: {
                //     title: "Open Street Map",
                //     value: D3.BingMaps.OpenStreetMap.GetTileSource()
                // },
                road: {
                    title: "Bing Maps Road",
                    value: Microsoft.Maps.MapTypeId.road,
                    selected: true
                },
                aerial: {
                    title: "Bing Maps Aerial",
                    value: Microsoft.Maps.MapTypeId.aerial
                },
            };

            self.$content.append($("<div></div>")
                .addClass("control-header")
                .text("Map style")
            );

            var _$mapTypeSelector = $("<div></div>")
                .addClass("map-type-selector")
                .appendTo(self.$content)
                .append($("<select></select>"));

            var _$opacitySliderTitle = $("<div></div>")
                .addClass("control-header")
                .text("Layer opacity")
                .appendTo(self.$content);

            var _$opacitySlider = $("<div></div>")
                .addClass("fc-time-slider transparency-slider")
                .appendTo(self.$content)
                .labeledSlider();

            var labels = FC.parseMatlabSequence("0:100");
            _$opacitySlider.setLabels(labels);

            self.$header = "Map options";
            self.$panel.addClass("map-options-panel right");

            Object.defineProperty(self, "$opacitySlider", {
                get: function () {
                    return _$opacitySlider;
                }
            });

            // Hide the panel on click outside of the panel.
            function onDocumentClick(event) {
                if (!self.$panel.is(event.target) && !self.$panel.has(event.target).length) {
                    self.hide();
                    $(document).off("click", onDocumentClick);
                }
            }

            self.initialize = function (map) {
                var $select = _$mapTypeSelector.find("select");

                for (var option in _mapTypes) {
                    var $option = $("<option></option>", {
                        value: option,
                        text: _mapTypes[option].title
                    });
                    $select.append($option);

                    if (_mapTypes[option].selected) {
                        $option.attr("selected", true);
                    }
                }

                $select.change(function () {
                    var mapType = _mapTypes[$(this).val()].value;
                    FC.mapEntities.setMapType(mapType);
                });
            };

            var _baseHide = self.hide;
            self.hide = function () {
                _baseHide(["slide", { direction: "right" }, 500]);
            };

            var _baseShow = self.show;
            self.show = function () {
                _$opacitySlider.toggle(FC.state.activePage === "results");
                _$opacitySliderTitle.toggle(FC.state.activePage === "results");
                _baseShow(["slide", { direction: "right" }, 500]);
                $(document).on("click touchend", onDocumentClick);
            };
        };
        Controls.MapOptionsPanel.prototype = new Controls.NamedPanel();

        Controls.MapSearchPanel = function (source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            var _lat, _lon, _name;

            var _$pointTemplate = $("#area-selection-point .area-point");

            var _$searchMessage = $("<div></div>").addClass("search-message");
            var _$searchProgress = $("<div></div>").addClass("search-progress");

            var _$searchInput = $("<input></input>")
                .addClass("search-input")
                .appendTo(self.$content)
                .after(_$searchMessage)
                .after(_$searchProgress);

            self.$header = "Find location";
            self.$panel.addClass("map-search-panel right");

            // Hide the panel on click outside of the panel.
            function onDocumentClick(event) {
                if (!self.$panel.is(event.target) && !self.$panel.has(event.target).length) {
                    self.hide();
                    $(document).off("click", onDocumentClick);
                }
            }

            // TODO: Add tile and update Client State (as in onPointModeComplete handler).
            function onAddPoint(event) {
                var mapPoint;

                if (_lat && _lon) {
                    mapPoint = FC.mapEntities.addPoint(_lat, _lon, {
                        name: _name,
                        isActive: true
                    });

                    self.$content.trigger("pointschanged", {
                        point: new FC.GeoPoint(_lat, _lon, _name),
                        mapPoint: mapPoint
                    });
                }
            }

            // NOTE: http://vivien-chevallier.com/Articles/use-bing-maps-rest-services-with-jquery-to-build-an-autocomplete-box-and-find-a-location-dynamically
            self.initialize = function () {
                var centerResult = function (item) {
                    _lat = item.point.coordinates[0];
                    _lon = item.point.coordinates[1];
                    _name = item.name;
                    FC.mapEntities.setView(_lat, _lon);
                };

                // Fix for iPad touch event.
                _$searchInput.on("blur.autocomplete", function (event) {
                    if ((/iPhone|iPod|iPad/i).test(navigator.userAgent)) {
                        event.stopImmediatePropagation();
                    }
                });

                _$searchInput.autocomplete({
                    delay: FC.Settings.MAP_SEARCH_DELAY,
                    minLength: 1,
                    source: function (request, response) {
                        // Reset stored coordinates.
                        _lat = _lon = null;
                        _$searchMessage.hide();
                        _$searchProgress.show();

                        // Make a request for new results.
                        $.ajax({
                            url: FC.Settings.BING_MAPS_LOCATIONS_SEARCH_URL,
                            dataType: "jsonp",
                            data: {
                                key: FC.Settings.BING_MAPS_API_KEY,
                                q: request.term
                            },
                            jsonp: "jsonp",
                            success: function (data) {
                                var result = data.resourceSets[0];
                                _$searchProgress.hide();
                                if (result && result.estimatedTotal > 0) {
                                    response($.map(result.resources, function (item) {
                                        return {
                                            data: item,
                                            label: item.name + " (" + item.address.countryRegion + ")",
                                            value: item.name
                                        };
                                    }));
                                }
                            },
                            error: function (data) {
                                _$searchProgress.hide();
                                _$searchMessage.show().text("The search service is unavailable. Please try again later.");
                            }
                        });
                    },
                    select: function (event, ui) {
                        centerResult(ui.item.data);
                        onAddPoint();
                    }
                });
            };

            var _baseHide = self.hide;
            self.hide = function () {
                _baseHide(["slide", {
                    direction: "right",
                    complete: function () {
                        _$searchMessage.hide();
                        _$searchProgress.hide();
                    }
                }, 500]);
            };

            var _baseShow = self.show;
            self.show = function () {
                _baseShow(["slide", {
                    direction: "right",
                    complete: function () {
                        _$searchInput.focus();
                    }
                }, 500]);
                $(document).on("click touchend", onDocumentClick);
            };
        };
        Controls.MapSearchPanel.prototype = new Controls.NamedPanel();

        Controls.MapAreaSelectionPanel = function MapAreaSelectionPanel(source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            var _$gridTemplate = $("#area-selection-grid .area-grid"),
                _$pointTemplate = $("#area-selection-point .area-point"),
                _$controls = self.$panel.find(".area-selection-panel-controls"),
                _$addGridBtn = _$controls.find(".add-grid-btn"),
                _$addPointBtn = _$controls.find(".add-point-btn"),
                _$clearAllBtn = _$controls.find(".remove-areas-btn"),
                _$panelMessage = self.$panel.find(".fc-panel-message"),
                _$separator = self.$panel.find(".separator-horizontal");

            Object.defineProperties(self, {
                $addGridButton: { get: function () { return _$addGridBtn; } },
                $addPointButton: { get: function () { return _$addPointBtn; } }
            });

            self.update = function () {
                var isEmpty = FC.state.grids.length === 0 && FC.state.points.length === 0;
                if (isEmpty) {
                    _$clearAllBtn.hide();
                    _$separator.hide();
                    _$panelMessage.text(FC.Settings.GEOGRAPHY_EMPTY_MESSAGE).show();
                } else {
                    _$clearAllBtn.show();
                    _$panelMessage.hide();
                    _$separator.show();
                }
            }

            var onRegionModeComplete = function (r) {
                var gridTile;

                if (!r) {
                    _$addGridBtn.removeClass("active");
                    return;
                }

                gridTile = new FC.Controls.AreaSelection.Grid({
                    source: _$gridTemplate.clone(true, true),
                    name: r.name,
                    mesh: {
                        latmin: r.min.latitude,
                        lonmin: r.min.longitude,
                        latmax: r.max.latitude,
                        lonmax: r.max.longitude,
                        latcount: 25,
                        loncount: 25
                    }
                });

                // gridTile.name = "Region " + (FC.state.grids.length + 1);

                // Adding new grid after last grid in a list if any
                if (self.$content.find(".area-grid").length) {
                    self.$content.find(".area-grid:last").after(gridTile.$area);
                }
                // otherwise adding before first point in a list if any
                else if (self.$content.find(".area-point").length) {
                    self.$content.find(".area-point:first").before(gridTile.$area);
                }
                // otherwise adding as first item in a list.
                else {
                    gridTile.appendTo(self.$content);
                }

                var grid = new FC.GeoGrid(
                    parseFloat(r.min.latitude),
                    parseFloat(r.max.latitude),
                    25,
                    parseFloat(r.min.longitude),
                    parseFloat(r.max.longitude),
                    25,
                    gridTile.name);
                self.$content.parent().nanoScroller();

                // Triggers that grids in client state to be updated.
                self.$panel.trigger("gridschanged", {
                    grid: grid,
                    gridTile: gridTile,
                    mapRegion: r
                });
            };

            var onPointModeComplete = function (p) {
                var pointTile;

                if (!p) {
                    _$addPointBtn.removeClass("active");
                    return;
                }

                pointTile = new FC.Controls.AreaSelection.Point({
                    source: _$pointTemplate.clone(true, true),
                    name: p.name,
                    mesh: {
                        lat: p.latitude,
                        lon: p.longitude
                    }
                });

                // pointTile.name = "Point " + (FC.state.points.length + 1);

                // Adding new point in the end of the list.
                pointTile.appendTo(self.$content);

                var point = new FC.GeoPoint(
                    parseFloat(p.latitude),
                    parseFloat(p.longitude),
                    pointTile.name);                    
                self.$content.parent().nanoScroller();

                // Triggers that points in client state to be updated.
                self.$panel.trigger("pointschanged", {
                    point: point,
                    pointTile: pointTile,
                    mapPoint: p
                });
            };

            _$addGridBtn.on("click", function addGrid () {
                _$addGridBtn.addClass("active");
                _$addPointBtn.removeClass("active");
                FC.mapEntities.toggleRegionMode(onRegionModeComplete);
            });

            _$addPointBtn.on("click", function addPoint () {
                _$addPointBtn.addClass("active");
                _$addGridBtn.removeClass("active");
                FC.mapEntities.togglePointMode(onPointModeComplete);
            });

            _$clearAllBtn.on("click", function removeAreas () {
                if (self.$content.find(".area-selection").length && window.confirm("Are you sure want to delete all selected areas?")) {
                    self.$content.empty();

                    FC.state.setGrids([]);
                    FC.state.setPoints([]);

                    FC.mapEntities.removeAll();

                    self.update();
                }
            });

            $(window).on("resize", function () {
                var paddingTop = parseFloat(self.$panel.css("padding-top").replace(/em|px|pt|%/, "")),
                    headerHeight = self.$header.outerHeight() +
                        parseFloat(self.$header.css("margin-top").replace(/em|px|pt|%/, "")) +
                        parseFloat(self.$header.css("margin-bottom").replace(/em|px|pt|%/, "")),
                    controlsHeight = self.$panel.find(".area-selection-panel-controls").outerHeight() +
                        parseFloat(self.$panel.find(".area-selection-panel-controls").css("margin-bottom").replace(/em|px|pt|%/, "")),
                    separatorHeight = self.$panel.find(".separator-horizontal:first").height(),

                    // Total height taken in panel.
                    heightTaken = paddingTop + headerHeight + controlsHeight + separatorHeight;

                self.$content.parent().nanoScroller();
                self.$content.parent().css("max-height", (self.$panel.outerHeight() - heightTaken) + "px");
                //self.$content.css("max-height", (self.$panel.outerHeight() - heightTaken) + "px");
            });

            // Initializating initial size of selected areas wrapper.
            $(window).trigger("resize");
        };
        Controls.MapAreaSelectionPanel.prototype = new Controls.NamedPanel();

        Controls.ResultsPanel = function ResultsPanel(source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            var _$controls = self.$panel.find(".fc-panel-controls");
            var _$layersTab = self.$panel.find(".tab.layers");
            var _$detailsTab = self.$panel.find(".tab.details");
            var _$layersTabContent = self.$panel.find(".tab-content.layers");
            var _$detailsTabContent = self.$panel.find(".tab-content.details");
            var _$dataInfoControl = self.$panel.find(".data-info-control");
            var _$timeChartControl = self.$panel.find(".time-chart-control");
            var _$tiles = self.$panel.find(".results-tiles");
            var _$probeToggleBtn = self.$panel.find(".toggle-probemode");
            var _$panelMessage = self.$panel.find(".fc-panel-message");
            var _$slicePanelToggleBtn = self.$panel.find(".toggle-slice-panel-btn");
            var _$slicePanelToggleBtnState = self.$panel.find(".toggle-slice-panel-btn-container b");
            var _dataInfoControl = new FC.Controls.DataInfoControl(_$dataInfoControl);
            var _timeChartControl = new FC.Controls.TimeChartControl(_$timeChartControl);
            var _isShowingSlicePanel = true;

            $(window).resize(function () {
                self.updateTabsHeight();
            });

            _$probeToggleBtn.on("click", function (event) {
                self.toggleProbeMode();
            });

            Object.defineProperties(self, {
                $controls: { get: function () { return _$controls; } },
                $layersTab: { get: function () { return _$layersTab; } },
                $detailsTab: { get: function () { return _$detailsTab; } },
                $layersTabContent: { get: function () { return _$layersTabContent; } },
                $detailsTabContent: { get: function () { return _$detailsTabContent; } },
                $tiles: { get: function () { return _$tiles; } },
                $selectedTile: { get: function () { return _$tiles.find(".selected"); } },
                $panelMessage: { get: function () { return _$panelMessage; } },
                selectedVariable: { get: function () { return self.$selectedTile.attr("data-variable"); } },
                dataInfoControl: { get: function () { return _dataInfoControl; } },
                timeChartControl: { get: function () { return _timeChartControl; } }
            });

            function initialize() {
                _dataInfoControl.$dataModeSelector.change(function (event, dataMode) {
                    FC.state.setDataMode(dataMode);
                });

                _timeChartControl.$timeSeriesAxisSelector.change(function (event, timeSeriesAxis) {
                    FC.state.setTimeSeriesAxis(timeSeriesAxis);
                });

                _$layersTab.click(function () {
                    _$layersTab.addClass("active");
                    _$detailsTab.removeClass("active");
                    _$detailsTabContent.parent().hide();
                    _$layersTabContent.parent().show();
                    self.updateTabsHeight();
                }).click();

                _$detailsTab.click(function () {
                    _$layersTab.removeClass("active");
                    _$detailsTab.addClass("active");
                    _$detailsTabContent.parent().show();
                    _$layersTabContent.parent().hide();
                    self.updateTabsHeight();
                });

                _$slicePanelToggleBtn.click(function () {
                    if (_isShowingSlicePanel) {
                        _$slicePanelToggleBtnState.text("Off");
                    }
                    else {
                        _$slicePanelToggleBtnState.text("On");
                    }

                    _$slicePanelToggleBtn.toggleClass("on");
                    _isShowingSlicePanel = !_isShowingSlicePanel;

                    self.$panel.trigger("showslicepanelchange", {
                        isShowing: _isShowingSlicePanel
                    });
                });
            }

            function getVariableDescription(variable) {
                return FC.state.config.EnvironmentalVariables.filter(function (v) {
                    return v.Name === variable;
                })[0].Description;
            }

            self.updateTabsHeight = function () {
                var height = self.$panel.height() - self.$header.outerHeight(true) -
                             self.$controls.outerHeight(true) - _$dataInfoControl.outerHeight(true);

                _$layersTabContent.parent().height(height);
                _$detailsTabContent.parent().height(height);

                _$layersTabContent.parent().nanoScroller();
                _$detailsTabContent.parent().nanoScroller();
            };

            self.selectFirstTile = function () {
                var $tiles = _$tiles.children();
                var variable = $tiles.first().attr("data-variable");
                self.selectTile(variable);
            };

            self.selectTile = function (variable) {
                _$tiles.children().removeClass("selected");
                _$tiles.find("[data-variable=" + variable + "]").click();
            };

            self.getTile = function (variable) {
                var $tile = _$tiles.find(".results-tile[data-variable=" + variable + "]");
                return $tile.data("tile");
            };

            self.addTile = function (variable) {
                var title = getVariableDescription(variable);
                var tile = new FC.Controls.ResultsTile(title, "", true);
                var $tile = tile.$tile;
                $tile.attr("data-variable", variable);
                $tile.data("tile", tile);
                _$tiles.append($tile);
            };

            self.removeTile = function (variable) {
                _$tiles.find("[data-variable=" + variable + "]").remove();
            };

            self.clearTiles = function () {
                _$tiles.empty();
            };

            self.toggleProbeMode = function () {
                if (FC.Map.probeMode) {
                    _$probeToggleBtn.removeClass("active")
                        .attr("title", "Enable probe mode");
                }
                else {
                    _$probeToggleBtn.addClass("active")
                        .attr("title", "Disable probe mode");
                }

                FC.Map.toggleProbeMode();
            };

            self.updatePanelMessage = function (message) {
                _$panelMessage.html(message);

                return self;
            };

            initialize();
        };
        Controls.ResultsPanel.prototype = new Controls.NamedPanel();

        Controls.TimeSlicePanel = function TimeSlicePanel(source) {
            var self = this;

            self.base = Controls.Panel;
            self.base(source);

            var _$sliceValue = self.$panel.find(".time-slice-value");
            var _$yearSlider = self.$panel.find(".fc-time-slider.year").yearSlider();
            var _$daySlider = self.$panel.find(".fc-time-slider.day").daySlider();
            var _$hourSlider = self.$panel.find(".fc-time-slider.hour").hourSlider();

            _$yearSlider.on("slidestart slide slidestop", onSliderFocus);
            _$daySlider.on("slidestart slide slidestop", onSliderFocus);
            _$hourSlider.on("slidestart slide slidestop", onSliderFocus);

            Object.defineProperties(self, {
                year: {
                    get: function () {
                        return {
                            $slider: _$yearSlider,
                            value: _$yearSlider.data("value"),
                            index: _$yearSlider.data("index"),
                            labels: _$yearSlider.data("labels"),
                            min: _$yearSlider.attr("data-min"),
                            max: _$yearSlider.attr("data-max"),
                            isVisible: _$yearSlider.is(":visible")
                        };
                    }
                },
                day: {
                    get: function () {
                        return {
                            $slider: _$daySlider,
                            value: _$daySlider.data("value"),
                            index: _$daySlider.data("index"),
                            labels: _$daySlider.data("labels"),
                            min: _$daySlider.attr("data-min"),
                            max: _$daySlider.attr("data-max"),
                            isVisible: _$daySlider.is(":visible")
                        };
                    }
                },
                hour: {
                    get: function () {
                        return {
                            $slider: _$hourSlider,
                            value: _$hourSlider.data("value"),
                            index: _$hourSlider.data("index"),
                            labels: _$hourSlider.data("labels"),
                            min: _$hourSlider.attr("data-min"),
                            max: _$hourSlider.attr("data-max"),
                            isVisible: _$hourSlider.is(":visible")
                        };
                    }
                }
            });

            function initialize() {
                 self.$panel.on("slicechange", function (event, data) {
                    data = event.data || data;

                    switch (data.type) {
                        case "year":
                            if (FC.state.yearSlice === data.index) {
                                if (event.stopPropagation) {
                                    event.stopPropagation();
                                }

                                return;
                            }

                            FC.state.setYearSlice(data.index);
                            _$yearSlider.setIndex(data.index);
                            break;
                        case "day":
                            if (FC.state.daySlice === data.index) {
                                if (event.stopPropagation) {
                                    event.stopPropagation();
                                }

                                return;
                            }

                            FC.state.setDaySlice(data.index);
                            _$daySlider.setIndex(data.index);
                            break;
                        case "hour":
                            if (FC.state.hourSlice === data.index) {
                                if (event.stopPropagation) {
                                    event.stopPropagation();
                                }

                                return;
                            }

                            FC.state.setHourSlice(data.index);
                            _$hourSlider.setIndex(data.index);
                            break;
                        default:
                            _$yearSlider.setIndex(FC.state.yearSlice);
                            _$daySlider.setIndex(FC.state.daySlice);
                            _$hourSlider.setIndex(FC.state.hourSlice);
                            break;
                    }
                });

                self.updateSliders();
                _$yearSlider.setIndex(FC.state.yearSlice);
                _$daySlider.setIndex(FC.state.daySlice);
                _$hourSlider.setIndex(FC.state.hourSlice);
            }

            function onSliderFocus() {
                FC.mapEntities.map.blur();
            }

            self.updateSliceValue = function () {
                var temporal = FC.state.temporal;
                var value = "Average value for ";
                if (temporal.hourCellMode && temporal.hours.length > 2 || !temporal.hourCellMode) {
                    value += _$hourSlider.data("value") + ", ";
                }
                if (temporal.dayCellMode && temporal.days.length > 2 || !temporal.dayCellMode) {
                    value += _$daySlider.data("value") + ", ";
                }
                value += _$yearSlider.data("value");
                _$sliceValue.text(value);
            };

            self.updateSliders = function () {
                var td = FC.state.temporal;
                var years = td.years.slice(0);
                var days = td.days.slice(0);
                var hours = td.hours.slice(0);
                var isLeapYear = (years.length === 1 || years.length === 2 && years[0] === years[1]) && FC.isLeapYear(years[0]);

                if (years.length > 2 && td.yearCellMode) {
                    _$yearSlider.show();
                    _$yearSlider.setRangeLabels(years);
                    if (!FC.state.yearSlice) {
                        self.$panel.trigger("slicechange", {
                            type: "year",
                            index: 0
                        });
                    }
                } else if (years.length > 1 && !td.yearCellMode) {
                    _$yearSlider.show();
                    _$yearSlider.setPointLabels(years);
                    if (!FC.state.yearSlice) {
                        self.$panel.trigger("slicechange", {
                            type: "year",
                            index: 0
                        });
                    }
                } else {
                    _$yearSlider.hide();
                    _$yearSlider.setSingleLabel(years);
                }

                if (days.length > 2 && td.dayCellMode) {
                    _$daySlider.show();
                    _$daySlider.setRangeLabels(days, isLeapYear);
                    if (!FC.state.daySlice) {
                        self.$panel.trigger("slicechange", {
                            type: "day",
                            index: 0
                        });
                    }
                } else if (days.length > 1 && !td.dayCellMode) {
                    _$daySlider.show();
                    _$daySlider.setPointLabels(days, isLeapYear);
                    if (!FC.state.daySlice) {
                        self.$panel.trigger("slicechange", {
                            type: "day",
                            index: 0
                        });
                    }
                } else {
                    _$daySlider.hide();
                    _$daySlider.setSingleLabel(days, isLeapYear);
                }

                if (hours.length > 2 && td.hourCellMode) {
                    _$hourSlider.show();
                    _$hourSlider.setRangeLabels(hours);
                    if (!FC.state.hourSlice) {
                        self.$panel.trigger("slicechange", {
                            type: "hour",
                            index: 0
                        });
                    }
                } else if (hours.length > 1 && !td.hourCellMode) {
                    _$hourSlider.show();
                    _$hourSlider.setPointLabels(hours);
                    if (!FC.state.hourSlice) {
                        self.$panel.trigger("slicechange", {
                            type: "hour",
                            index: 0
                        });
                    }
                } else {
                    _$hourSlider.hide();
                    _$hourSlider.setSingleLabel(hours);
                }

                _$yearSlider.setIndex(FC.state.yearSlice);
                _$daySlider.setIndex(FC.state.daySlice);
                _$hourSlider.setIndex(FC.state.hourSlice);

                self.updateSliceValue();
            };

            initialize();
        };
        Controls.TimeSlicePanel.prototype = new Controls.Panel();

        Controls.ExportPanel = function ExportPanel (source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            var _$dataToCSVBtn = self.$content.find(".action-data-to-file"),
                _$dataByEmailBtn = self.$content.find(".action-send-by-email"),
                _$dataCopyURLBtn = self.$content.find(".action-copy-url"),
                _$copyURLDialog = self.$panel.parent().find(".copy-to-clipboard-dialog"),
                _$copyURLDialogOverlay = self.$panel.parent().find(".dialog-overlay"),
                _$copyURLDialogInput = _$copyURLDialog.find(".url-input"),
                _$copyURLDialogCloseButton = _$copyURLDialog.find(".close-button"),
                _$panelMessage = self.$panel.find(".fc-panel-message"),
                _$panelContent = self.$panel.find(".fc-panel-content");

            _$dataToCSVBtn.on("click", onDataToCSVClicked);
            _$dataByEmailBtn.on("click", onDataByEmailClicked);
            _$dataCopyURLBtn.on("click", onDataCopyURLClicked);
            _$copyURLDialogCloseButton.add(_$copyURLDialogOverlay).click(hideCopyUrlDialog);
            _$copyURLDialogInput.focus(onCopyUrlDialogInputFocus);

            self.update = function () {
                // No grid or point selected, show 'no area selected' message.
                if (FC.state.grids.length === 0 && FC.state.points.length === 0) {
                    _$panelMessage.html(FC.Settings.NO_AREA_SELECTED_MESSAGE).show();
                    _$panelContent.hide();
                } else if ($.isEmptyObject(FC.state.variables)) { // No layer selected, show 'no layer selected' message.
                    _$panelMessage.html(FC.Settings.NO_LAYERS_SELECTED_MESSAGE).show();
                    _$panelContent.hide();
                } else {
                    var url = createExportUrl();
                    if (url !== FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/export") {
                        _$panelMessage.hide();
                        _$panelContent.show();
                    } else {
                        _$panelMessage.html(FC.Settings.NO_DATA_AVAILABLE_MESSAGE).show();
                        _$panelContent.hide();
                    }
                }
            }

            function onDataToCSVClicked () {
                var url = createExportUrl();

                if (url !== FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/export") FC.openUrlInNewTab(url);
            }

            function onDataByEmailClicked () {
                var url = createExportUrl();

                if (url !== FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/export") {
                    $(this).attr("href", "mailto:?subject=" + FC.Settings.DEFAULT_EMAIL_SUBJECT + "&body=" + url);
                }
            }

            function onDataCopyURLClicked () {
                var url = createExportUrl();

                if (url !== FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/export") showCopyURLDialog(url);
            }

            function createExportUrl () {                
                var url = FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/export",
                    includeGrids = false,
                    includeGrid = false,
                    firstParam = true,
                    request,
                    pointsHashes = {}; // hash-map of arrays of points names, keys are hashes of points requests

                if (FC.state.grids.length) {
                    FC.state.grids.forEach(function (grid) {
                        includeGrid = false;

                        if (grid.data) {
                            if (Object.keys(grid.data).length > 0) {
                                Object.keys(grid.data).forEach(function (key) {
                                    request = grid.data[key].request;

                                    // Only grid with at least one variable with request in status 'receiving' or 'completed' can be added to export url.
                                    if (request.status === "receiving" || request.status === "completed") {
                                        // First grid to be added to url, adding grids parameter to url.
                                        if (!includeGrids) {
                                            url += "?g=";
                                            includeGrids = true;
                                            firstParam = false;
                                        }

                                        // First variable of current grid to be added to url, adding grid's name to url.
                                        if (!includeGrid) {
                                            url += encodeURIComponent(grid.name);
                                            includeGrid = true;
                                        }

                                        url += "," + request.hash;
                                    }
                                });
                            }

                            if (includeGrid) {
                                url += ";";
                            }
                        }
                    });
                }

                if (FC.state.points.length) {
                    FC.state.points.forEach(function (point) {
                        if (point.data) {
                            if (Object.keys(point.data).length > 0) {
                                Object.keys(point.data).forEach(function (key) {
                                    request = point.data[key].request;

                                    // Only point with at least one variable with request in status 'receiving' or 'completed' can be added to export url.
                                    if (request.status === "receiving" || request.status === "completed") {
                                        pointsHashes[request.hash] = pointsHashes[request.hash] || [];
                                        pointsHashes[request.hash].push(encodeURIComponent(point.name));
                                    }
                                });
                            }
                        }
                    });

                    if (!$.isEmptyObject(pointsHashes)) {
                        url += firstParam ? "?p=" : "&p=";

                        Object.keys(pointsHashes).forEach(function (key) {
                            url += key + "," + pointsHashes[key].join(",") + ";";
                        });
                    }
                }

                // Email clients parse %20 as space and break URL. Replace %20 with +.
                url = url.replace(/%20/gi, "+");

                return url;
            }

            function showCopyURLDialog(url) {
                _$copyURLDialog.show();
                _$copyURLDialogOverlay.show();
                _$copyURLDialogInput.val(url);
                _$copyURLDialogInput.focus();
            }

            function hideCopyUrlDialog() {
                _$copyURLDialog.hide();
                _$copyURLDialogOverlay.hide();
                _$copyURLDialogInput.val("");
            }

            function onCopyUrlDialogInputFocus() {
                this.select();
            }
        };
        Controls.ExportPanel.prototype = new Controls.NamedPanel();

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);