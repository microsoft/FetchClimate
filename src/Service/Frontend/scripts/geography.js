// TODO: merge code for d3 map from main.js

(function (FC, $) {
    (function (Geography) {
        "use strict";

        var _$page, // geography section container

            _areaSelectionPanel,

            _editingArea,
            _areaSelectionGrids = [],
            _areaSelectionPoints = [];

        /**
         * Initializes geography section.
         */
        Geography.initialize = function () {
            _$page = $("section.geography");

            _areaSelectionPanel = new FC.Controls.MapAreaSelectionPanel($(".map-area-selection-panel"));

            var gridTemplate = $("#area-selection-grid .area-grid"),
                pointTemplate = $("#area-selection-point .area-point");

            _areaSelectionPanel.update();

            FC.state.grids.forEach(function (grid) {
                var $grid = new FC.Controls.AreaSelection.Grid({
                    source: gridTemplate.clone(true, true),
                    name: decodeURIComponent(grid.name),
                    mesh: {
                        latmin: grid.latmin,
                        lonmin: grid.lonmin,
                        latmax: grid.latmax,
                        lonmax: grid.lonmax,
                        latcount: grid.latcount,
                        loncount: grid.loncount
                    },
                    id: grid.id
                });

                $grid.appendTo(_areaSelectionPanel.$content);
                FC.mapEntities.addRegion(grid.latmin, grid.lonmin, grid.latmax, grid.lonmax, {
                    id: grid.id,
                    name: decodeURIComponent(grid.name)
                });
                _areaSelectionGrids.push($grid);
            });

            FC.state.points.forEach(function (point) {
                var $point = new FC.Controls.AreaSelection.Point({
                    source: pointTemplate.clone(true, true),
                    name: decodeURIComponent(point.name),
                    mesh: {
                        lat: point.lat,
                        lon: point.lon
                    },
                    id: point.id
                });

                $point.appendTo(_areaSelectionPanel.$content);
                FC.mapEntities.addPoint(point.lat, point.lon, {
                    id: point.id,
                    name: decodeURIComponent(point.name)
                });
                _areaSelectionPoints.push($point);
            });

            // Update height of panel's content after elements become visible, using setTimeout hack for it.
            setTimeout(function () {
                $(window).trigger("resize");
            }, 0);

            _$page.on("editingareachanged", function (event, area) {
                if (_editingArea && _editingArea !== area && _editingArea.$area.hasClass("editing")) {
                    _editingArea.toggleEdit();
                }

                // Update nanoScroller when tile animation will end.
                setTimeout(function () {
                    _areaSelectionPanel.$content.parent().nanoScroller();
                }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                _editingArea = area;
            });

            _$page.on("gridschanged", function (event, param) {
                // Delete all grids.
                if (typeof param === "undefined") {
                    FC.state.setGrids([]);

                    _areaSelectionGrids = [];
                }
                // Delete grid by given id.
                else if (typeof param.id !== "undefined" && typeof param.grid === "undefined") {
                    FC.state.deleteGrid(param.id);

                    _areaSelectionGrids = _areaSelectionGrids.filter(function (grid) {
                        return grid.id !== param.id;
                    });

                    FC.mapEntities.removeRegion(param.id);
                }
                // Update grid by given id.
                else if (typeof param.id !== "undefined" && typeof param.grid !== "undefined") {
                    FC.state.setGrid(param.id, param.grid);

                    FC.mapEntities.unselectRegion(param.id);
                    FC.mapEntities.updateRegion(param.id, {
                        min: {
                            latitude: param.grid.latmin,
                            longitude: param.grid.lonmin
                        },
                        max: {
                            latitude: param.grid.latmax,
                            longitude: param.grid.lonmax
                        },
                        name: param.grid.name
                    });
                }
                // Add new grid.
                else if (typeof param.id === "undefined" && typeof param.grid !== "undefined") {
                    var id = FC.state.addGrid(param.grid);

                    if (typeof param.gridTile !== "undefined") {
                        param.gridTile.id = id;
                        param.gridTile.$area.attr("data-id", id);
                        _areaSelectionGrids.push(param.gridTile);
                    }

                    if (typeof param.mapRegion !== "undefined") {
                        param.mapRegion.setId(id);
                    }

                    Geography.selectGrid(id);
                }

                _areaSelectionPanel.update();

                if (event.stopPropagation) event.stopPropagation();
            });

            _$page.on("pointschanged", function (event, param) {
                // Delete all points.
                if (typeof param === "undefined") {
                    FC.state.setPoints([]);

                    _areaSelectionPoints = [];
                }
                // Delete point by given id.
                else if (typeof param.id !== "undefined" && typeof param.point === "undefined") {
                    FC.state.deletePoint(param.id);

                    _areaSelectionPoints = _areaSelectionPoints.filter(function (point) {
                        return point.id !== param.id;
                    });

                    FC.mapEntities.removePoint(param.id);
                }
                // Update point by given id.
                else if (typeof param.id !== "undefined" && typeof param.point !== "undefined") {
                    FC.state.setPoint(param.id, param.point);

                    FC.mapEntities.updatePoint(param.id, {
                        latitude: param.point.lat,
                        longitude: param.point.lon,
                        name: param.point.name
                    });
                    FC.mapEntities.unselectPoint(param.id);
                }
                // Add new point.
                else if (typeof param.id === "undefined" && typeof param.point !== "undefined") {
                    var id = FC.state.addPoint(param.point),
                        pointTile;

                    // Point created by click on map.
                    if (typeof param.pointTile !== "undefined") {
                        pointTile = param.pointTile;
                    }
                    // Point created by click on search result.
                    else {
                        pointTile = new FC.Controls.AreaSelection.Point({
                            source: pointTemplate.clone(true, true),
                            name: param.point.name,
                            mesh: {
                                lat: param.point.lat,
                                lon: param.point.lon
                            },
                            id: id
                        });
                        pointTile.appendTo(_areaSelectionPanel.$content);
                    }

                    pointTile.id = id;
                    pointTile.$area.attr("data-id", id);
                    _areaSelectionPoints.push(pointTile);

                    if (typeof param.mapPoint !== "undefined") {
                        param.mapPoint.setId(id);
                    }

                    Geography.selectPoint(id);
                }

                _areaSelectionPanel.update();

                if (event.stopPropagation) event.stopPropagation();
            });

            FC.state.on("activepagechange", function (page) {
                if (page === "geography" || page === "results") {
                    if (_areaSelectionPanel.$addPointButton.hasClass("active")) {
                        _areaSelectionPanel.$addPointButton.click();
                    }
                    if (_areaSelectionPanel.$addGridButton.hasClass("active")) {
                        _areaSelectionPanel.$addGridButton.click();
                    }
                }
            });
        };

        /**
         * Activates edit mode for grid tile with given id and scrolls to it.
         */
        Geography.selectGrid = function (id) {
            var gridTile = _areaSelectionGrids.filter(function (grid) {
                return id === grid.id;
            })[0];

            if (!gridTile) {
                return false;
            }

            if (_editingArea) {
                if (_editingArea !== gridTile) {
                    if (_editingArea.$area.hasClass("editing")) {
                        _editingArea.toggleEdit();
                    }

                    gridTile.toggleEdit();
                }
                else if (_editingArea.id === gridTile.id && !_editingArea.$area.hasClass("editing")) {
                    gridTile.toggleEdit();
                }
            }
            else {
                gridTile.toggleEdit();
            }

            _editingArea = gridTile;

            _areaSelectionPanel.$content.parent().nanoScroller({ scrollTo: $(".area-selection.area-grid[data-id='" + id + "']") });
        };

        /**
         * Dectivates edit mode for grid tile with given it.
         */
        Geography.unselectGrid = function (id) {
            var gridTile = _areaSelectionGrids.filter(function (grid) {
                return id === grid.id;
            })[0];

            if (!gridTile) {
                return false;
            }

            if (_editingArea && _editingArea === gridTile && _editingArea.$area.hasClass("editing")) {
                gridTile.toggleEdit();
                _editingArea = null;
            }
        };

        /**
         * Updates coordinates for grid (tile) with given id.
         * Also updates client state if updateHash is set to true.
         */
        Geography.updateGrid = function (id, region, updateHash) {
            var gridTile = _areaSelectionGrids.filter(function (grid) {
                return id === grid.id;
            })[0];

            if (!gridTile) {
                return false;
            }

            gridTile.updateCoordinates(region, updateHash);
        };

        /**
         * Activates edit mode for point tile with given id and scrolls to it.
         */
        Geography.selectPoint = function (id) {
            var pointTile = _areaSelectionPoints.filter(function (point) {
                return id === point.id;
            })[0];

            if (!pointTile) {
                return false;
            }

            if (_editingArea) {
                if (_editingArea !== pointTile) {
                    if (_editingArea.$area.hasClass("editing")) {
                    _editingArea.toggleEdit();
                    }
                    
                    pointTile.toggleEdit();
                }
                else if (_editingArea.id === pointTile.id && !_editingArea.$area.hasClass("editing")) {
                    pointTile.toggleEdit();
                }
            }
            else {
                pointTile.toggleEdit();
            }

            _editingArea = pointTile;

            _areaSelectionPanel.$content.parent().nanoScroller({ scrollTo: $(".area-selection.area-point[data-id='" + id + "']") });
        };

        /**
         * Dectivates edit mode for point tile with given it.
         */
        Geography.unselectPoint = function (id) {
            var pointTile = _areaSelectionPoints.filter(function (point) {
                return id === point.id;
            })[0];

            if (!pointTile) {
                return false;
            }

            if (_editingArea && _editingArea === pointTile && _editingArea.$area.hasClass("editing")) {
                pointTile.toggleEdit();
                _editingArea = null;
            }
        };

        /**
         * Updates coordinates for point (tile) with given id.
         * Also updates client state if updateHash is set to true.
         */
        Geography.updatePoint = function (id, point, updateHash) {
            var pointTile = _areaSelectionPoints.filter(function (_point) {
                return id === _point.id;
            })[0];

            if (!pointTile) {
                return false;
            }

            pointTile.updateCoordinates(point, updateHash);
        };

    })(FC.Geography || (FC.Geography = {}));
})(window.FC = window.FC || {}, jQuery);