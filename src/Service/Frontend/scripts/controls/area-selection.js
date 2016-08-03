(function (FC, $) {
    (function (Controls) {
        (function (AreaSelection) {
        "use strict";

        /**
         * Represents editable grid cell in a list of all selected areas on map.
         */
        AreaSelection.Grid = function Grid (data) {
            var self = this;

            var _$grid = data.source,
                _$name = data.source.find(".area-name"),
                _$nameInput = data.source.find(".area-name-input"),
                _$shorten = data.source.find(".area-shorten"), // contains short info about selected coords and mesh

                _$coordinatesTable = data.source.find(".area-coords-table"), // wrapper for coordinates input
                _$latmin = _$coordinatesTable.find(".area-coords-row:nth-child(1) .area-coords-input:first"),
                _$lonmin = _$coordinatesTable.find(".area-coords-row:nth-child(1) .area-coords-input:last"),
                _$latmax = _$coordinatesTable.find(".area-coords-row:nth-child(2) .area-coords-input:first"),
                _$lonmax = _$coordinatesTable.find(".area-coords-row:nth-child(2) .area-coords-input:last"),
                _$latcount = _$coordinatesTable.find(".area-coords-row:nth-child(3) .area-coords-input:first"),
                _$loncount = _$coordinatesTable.find(".area-coords-row:nth-child(3) .area-coords-input:last"),

                _id = data.id;

            Object.defineProperties(self, {
                "$area": {
                    get: function () {
                        return _$grid;
                    }
                },
                "name": {
                    get: function () {
                        return _$name.text();
                    },
                    set: function (name) {
                        _$name.text(name);
                        _$nameInput.val(name);
                        _$nameInput.data("original", name);
                    }
                },
                "id": {
                    get: function () {
                        return _id;
                    },
                    set: function (id) {
                        _id = id;
                    }
                }
            });

            // Initialization

            _$latmin.val(FC.roundTo(data.mesh.latmin, FC.Settings.DISPLAY_PRECISION));
            _$lonmin.val(FC.roundTo(data.mesh.lonmin, FC.Settings.DISPLAY_PRECISION));
            _$latmax.val(FC.roundTo(data.mesh.latmax, FC.Settings.DISPLAY_PRECISION));
            _$lonmax.val(FC.roundTo(data.mesh.lonmax, FC.Settings.DISPLAY_PRECISION));
            _$latcount.val(data.mesh.latcount);
            _$loncount.val(data.mesh.loncount);
            _$nameInput.val(data.name);
            _$name.text(data.name);
            _$grid.attr("data-id", data.id);

            _$grid.find("input").each(function () {
                $(this).data("original", $(this).val());
            });

            function getShortHtml() {
                return "Min Lat: " + FC.roundTo(_$latmin.val(), FC.Settings.DISPLAY_PRECISION) + " Lon: " + FC.roundTo(_$lonmin.val(), FC.Settings.DISPLAY_PRECISION) + "</br>" +
                "Max Lat: " + FC.roundTo(_$latmax.val(), FC.Settings.DISPLAY_PRECISION) + " Lon: " + FC.roundTo(_$lonmax.val(), FC.Settings.DISPLAY_PRECISION) + "</br>" +
                "Resolution Lat: " + _$latcount.val() + " Lon: " + _$loncount.val();
            }

            function htmlToGeoGrid (region) {
                var latmin = region && typeof region.min.latitude !== "undefined" ? region.min.latitude : parseFloat(_$latmin.val()),
                    latmax = region && typeof region.max.latitude !== "undefined" ? region.max.latitude : parseFloat(_$latmax.val()),
                    latcount = _$latcount.val(),
                    lonmin = region && typeof region.min.longitude !== "undefined" ? region.min.longitude : parseFloat(_$lonmin.val()),
                    lonmax = region && typeof region.max.longitude !== "undefined" ? region.max.longitude : parseFloat(_$lonmax.val()),
                    loncount = _$loncount.val(),
                    name = _$nameInput.val();

                return new FC.GeoGrid(
                    latmin, latmax, latcount,
                    lonmin, lonmax, loncount,
                    name);
            }

            _$shorten.html(getShortHtml()).addClass("active");
            _$grid.find(".remove-area-btn").show();
            _$grid.find(".toggle-edit-btn").show();


            self.appendTo = function ($parent) {
                _$grid.appendTo($parent);
            };

            self.remove = function () {
                _$grid.remove();
            };

            /**
             * Toggles edit mode for grid cell.
             * In active edit mode coordinates input, grid's name input, cancel and update buttons are displayed.
             * In inactive edit mode grid's name, shorten for selected coordinates, save and remove buttons are displayed.
             */
            self.toggleEdit = function toggleEdit () {
                var height;

                _$shorten.stop(true);
                _$coordinatesTable.stop(true);

                if (_$grid.hasClass("editing")) {
                    _$shorten.html(getShortHtml())
                        .addClass("active");
                    _$coordinatesTable.removeClass("active");

                    height = _$shorten.css("height", "auto").height();
                    _$shorten.height(0).animate({
                        height: height
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$coordinatesTable.animate({
                        height: 0
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$grid.removeClass("editing");

                    _$nameInput.hide();
                    _$name.show();

                    _$grid.find(".save-btn").hide();
                    _$grid.find(".toggle-edit-btn").show();

                    FC.mapEntities.unselectRegion(_id);
                }
                else {
                    _$shorten.removeClass("active");
                    _$coordinatesTable.addClass("active");
                    _$grid.addClass("editing");

                    height = _$coordinatesTable.css("height", "auto").height();
                    _$coordinatesTable.height(0).animate({
                        height: height
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$shorten.animate({
                        height: 0
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$nameInput.show();
                    _$name.hide();

                    _$grid.find(".save-btn").show();
                    _$grid.find(".toggle-edit-btn").hide();

                    // Triggers currently editing area cell.
                    _$grid.trigger("editingareachanged", self);

                    FC.mapEntities.selectRegion(_id);
                }
            };

            self.updateCoordinates = function (region, updateHash) {
                var grid;

                _$latmin.val(FC.roundTo(region.min.latitude, FC.Settings.DISPLAY_PRECISION));
                _$lonmin.val(FC.roundTo(region.min.longitude, FC.Settings.DISPLAY_PRECISION));
                _$latmax.val(FC.roundTo(region.max.latitude, FC.Settings.DISPLAY_PRECISION));
                _$lonmax.val(FC.roundTo(region.max.longitude, FC.Settings.DISPLAY_PRECISION));

                _$grid.find("input").each(function () {
                    $(this).data("original", $(this).val());
                });

                grid = htmlToGeoGrid(region);

                // Update client state.
                if (updateHash) FC.state.setGrid(_id, grid);
            };

            _$grid.find(".save-btn").on("click", function () {
                var id = parseInt(_$grid.attr("data-id")),
                    grid = htmlToGeoGrid();

                _$grid.find("input").each(function () {
                    $(this).data("original", $(this).val());
                });

                self.name = _$nameInput.val();
                self.toggleEdit();

                // Triggers that grids in client state to be updated.
                _$grid.trigger("gridschanged", {
                    id: id,
                    grid: grid
                });
            });

            _$grid.find(".cancel-btn").on("click", function () {
                _$grid.find("input").each(function () {
                    $(this).val($(this).data("original"));
                });

                self.toggleEdit();

                FC.mapEntities.unselectRegion(_id);
            });

            _$grid.find(".toggle-edit-btn").on("click", function () {
                self.toggleEdit();
            });

            _$grid.find(".remove-area-btn").on("click", function () {
                var id = parseInt(_$grid.attr("data-id"));

                _$grid.removeClass("area-grid")

                    // Triggers that grids in client state to be updated.
                    .trigger("gridschanged", {
                        id: id
                    });

                self.remove();
            });
        };

        /**
         * Represents editable point cell in a list of all selected areas on map.
         */
        AreaSelection.Point = function Point (data) {
            var self = this;

            var _$point = data.source,
                _$name = data.source.find(".area-name"),
                _$nameInput = data.source.find(".area-name-input"),
                _$shorten = data.source.find(".area-shorten"),

                _$coordinatesTable = data.source.find(".area-coords-table"),
                _$lat = _$coordinatesTable.find(".area-coords-row .area-coords-input:first"),
                _$lon = _$coordinatesTable.find(".area-coords-row .area-coords-input:last"),

                _id = data.id;

            // Initialization.

            _$lat.val(FC.roundTo(data.mesh.lat, FC.Settings.DISPLAY_PRECISION));
            _$lon.val(FC.roundTo(data.mesh.lon, FC.Settings.DISPLAY_PRECISION));
            _$nameInput.val(data.name);
            _$name.text(data.name);
            _$point.attr("data-id", data.id);

            _$shorten.html(getShortHtml())
                .addClass("active");

            _$point.find(".remove-area-btn").show();
            _$point.find(".toggle-edit-btn").show();

            _$point.find("input").each(function () {
                $(this).data("original", $(this).val());
            });
            

            function getShortHtml() {
                return "Lat: " + FC.roundTo(_$lat.val(), FC.Settings.DISPLAY_PRECISION) +
                       " Lon: " + FC.roundTo(_$lon.val(), FC.Settings.DISPLAY_PRECISION);
            }

            Object.defineProperties(self, {
                "$area": {
                    get: function () {
                        return _$point;
                    }
                },
                "name": {
                    get: function () {
                        return _$name.text();
                    },
                    set: function (name) {
                        _$name.text(name);
                        _$nameInput.val(name);
                        _$nameInput.data("original", name);
                    }
                },
                "id": {
                    get: function () {
                        return _id;
                    },
                    set: function (id) {
                        _id = id;
                    }
                }
            });

            function htmlToGeoPoint (point) {
                var lat = point && typeof point.latitude !== "undefined" ? point.latitude : parseFloat(_$lat.val()),
                    lon = point && typeof point.longitude !== "undefined" ? point.longitude : parseFloat(_$lon.val()),
                    name = _$nameInput.val();

                return new FC.GeoPoint(lat, lon, name);
            }

            self.appendTo = function ($parent) {
                _$point.appendTo($parent);
            };

            self.remove = function () {
                _$point.remove();
            };

            /**
             * Toggles edit mode for point cell.
             * In active edit mode coordinates input, point's name input, cancel and update buttons are displayed.
             * In inactive edit mode point's name, shorten for selected coordinates, save and remove buttons are displayed.
             */
            self.toggleEdit = function toggleEdit () {
                var height;

                _$shorten.stop(true);
                _$coordinatesTable.stop(true);

                if (_$point.hasClass("editing")) {
                    _$shorten.html(getShortHtml())
                        .addClass("active");
                    _$coordinatesTable.removeClass("active");
                    _$point.removeClass("editing");

                    height = _$shorten.css("height", "auto").height();
                    _$shorten.height(0).animate({
                        height: height
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$coordinatesTable.animate({
                        height: 0
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$name.show();
                    _$nameInput.hide();

                    _$point.find(".save-btn").hide();
                    _$point.find(".toggle-edit-btn").show();

                    FC.mapEntities.unselectPoint(_id);
                }
                else {
                    _$shorten.removeClass("active");
                    _$coordinatesTable.addClass("active");
                    _$point.addClass("editing");

                    height = _$coordinatesTable.css("height", "auto").height();
                    _$coordinatesTable.height(0).animate({
                        height: height
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$shorten.animate({
                        height: 0
                    }, FC.Settings.GEOGRAPHY_TILE_SLIDE_TIME);

                    _$name.hide();
                    _$nameInput.show();
                    _$nameInput.data("original", data.name);

                    _$point.find(".save-btn").show();
                    _$point.find(".toggle-edit-btn").hide();

                    // Triggers currently editing area cell.
                    _$point.trigger("editingareachanged", self);

                    FC.mapEntities.selectPoint(_id);
                }
            };

            self.updateCoordinates = function (point, updateHash) {
                var point;

                _$lat.val(FC.roundTo(point.latitude, FC.Settings.DISPLAY_PRECISION));
                _$lon.val(FC.roundTo(point.longitude, FC.Settings.DISPLAY_PRECISION));

                _$point.find("input").each(function () {
                    $(this).data("original", $(this).val());
                });

                point = htmlToGeoPoint(point);

                // Triggers that grids in client state to be updated.
                if (updateHash) FC.state.setPoint(_id, point);
            };

            _$point.find(".save-btn").on("click", function onClick () {
                var id = parseInt(_$point.attr("data-id")),
                    point = htmlToGeoPoint();

                _$point.find("input").each(function () {
                    $(this).data("original", $(this).val());
                });

                self.name = _$nameInput.val();
                self.toggleEdit();

                // Triggers that points in client state to be updated.
                _$point.trigger("pointschanged", {
                    id: id,
                    point: point
                });
            });

            _$point.find(".cancel-btn").on("click", function onClick () {                
                _$point.find("input").each(function () {
                    $(this).val($(this).data("original"));
                });
                
                self.toggleEdit();
            });

            _$point.find(".toggle-edit-btn").on("click", function onClick () {
                self.toggleEdit();
            });

            _$point.find(".remove-area-btn").on("click", function onClick () {
                var id = parseInt(_$point.attr("data-id"));

                _$point.removeClass("area-point")

                    // Triggers that points in client state to be updated.
                    .trigger("pointschanged", {
                        id: id
                    });

                self.remove();
            });
        };

        })(Controls.AreaSelection || (Controls.AreaSelection = {}));
    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);