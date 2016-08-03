(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.LayersSelectionPanel = function LayersSelectionPanel(source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            //var _$gridTemplate = $("#area-selection-grid .area-grid"),
            //    _$pointTemplate = $("#area-selection-point .area-point"),
            var    _$controls = self.$panel.find(".layers-selection-panel-controls"),            
            //    _$addGridBtn = _$controls.find(".add-grid-btn"),
            //    _$addPointBtn = _$controls.find(".add-point-btn"),
                  _$clearAllBtn = _$controls.find(".remove-layers-btn"),
                  _$panelMessage = self.$panel.find(".fc-panel-message"),
                  _$separator = self.$panel.find(".separator-horizontal");

            //Object.defineProperties(self, {
            //    $addGridButton: { get: function () { return _$addGridBtn; } },
            //    $addPointButton: { get: function () { return _$addPointBtn; } }
            //});

            self.update = function () {
                var isEmpty = Object.keys(FC.state.variables).length === 0;
                if (isEmpty) {
                    _$clearAllBtn.hide();
                    _$separator.hide();
                    _$panelMessage.text(FC.Settings.LAYERS_EMPTY_MESSAGE).show();
                } else {
                    _$clearAllBtn.show();
                    _$panelMessage.hide();
                    _$separator.show();
                }
            }

            //var onRegionModeComplete = function (r) {
            //    var gridTile;

            //    if (!r) {
            //        _$addGridBtn.removeClass("active");
            //        return;
            //    }

            //    gridTile = new FC.Controls.AreaSelection.Grid({
            //        source: _$gridTemplate.clone(true, true),
            //        name: r.name,
            //        mesh: {
            //            latmin: r.min.latitude,
            //            lonmin: r.min.longitude,
            //            latmax: r.max.latitude,
            //            lonmax: r.max.longitude,
            //            latcount: 25,
            //            loncount: 25
            //        }
            //    });

            //    // gridTile.name = "Region " + (FC.state.grids.length + 1);

            //    // Adding new grid after last grid in a list if any
            //    if (self.$content.find(".area-grid").length) {
            //        self.$content.find(".area-grid:last").after(gridTile.$area);
            //    }
            //        // otherwise adding before first point in a list if any
            //    else if (self.$content.find(".area-point").length) {
            //        self.$content.find(".area-point:first").before(gridTile.$area);
            //    }
            //        // otherwise adding as first item in a list.
            //    else {
            //        gridTile.appendTo(self.$content);
            //    }

            //    var grid = new FC.GeoGrid(
            //        parseFloat(r.min.latitude),
            //        parseFloat(r.max.latitude),
            //        25,
            //        parseFloat(r.min.longitude),
            //        parseFloat(r.max.longitude),
            //        25,
            //        gridTile.name);
            //    self.$content.parent().nanoScroller();

            //    // Triggers that grids in client state to be updated.
            //    self.$panel.trigger("gridschanged", {
            //        grid: grid,
            //        gridTile: gridTile,
            //        mapRegion: r
            //    });
            //};

            //var onPointModeComplete = function (p) {
            //    var pointTile;

            //    if (!p) {
            //        _$addPointBtn.removeClass("active");
            //        return;
            //    }

            //    pointTile = new FC.Controls.AreaSelection.Point({
            //        source: _$pointTemplate.clone(true, true),
            //        name: p.name,
            //        mesh: {
            //            lat: p.latitude,
            //            lon: p.longitude
            //        }
            //    });

            //    // pointTile.name = "Point " + (FC.state.points.length + 1);

            //    // Adding new point in the end of the list.
            //    pointTile.appendTo(self.$content);

            //    var point = new FC.GeoPoint(
            //        parseFloat(p.latitude),
            //        parseFloat(p.longitude),
            //        pointTile.name);
            //    self.$content.parent().nanoScroller();

            //    // Triggers that points in client state to be updated.
            //    self.$panel.trigger("pointschanged", {
            //        point: point,
            //        pointTile: pointTile,
            //        mapPoint: p
            //    });
            //};

            //_$addGridBtn.on("click", function addGrid() {
            //    _$addGridBtn.addClass("active");
            //    _$addPointBtn.removeClass("active");
            //    FC.mapEntities.toggleRegionMode(onRegionModeComplete);
            //});

            //_$addPointBtn.on("click", function addPoint() {
            //    _$addPointBtn.addClass("active");
            //    _$addGridBtn.removeClass("active");
            //    FC.mapEntities.togglePointMode(onPointModeComplete);
            //});

            //_$clearAllBtn.on("click", function removeAreas() {
            //    if (self.$content.find(".area-selection").length && window.confirm("Are you sure want to delete all selected areas?")) {
            //        self.$content.empty();

            //        FC.state.setGrids([]);
            //        FC.state.setPoints([]);

            //        FC.mapEntities.removeAll();

            //        self.update();
            //    }
            //});

            $(window).on("resize", function () {
                var paddingTop = parseFloat(self.$panel.css("padding-top").replace(/em|px|pt|%/, "")),
                    headerHeight = self.$header.outerHeight() +
                        parseFloat(self.$header.css("margin-top").replace(/em|px|pt|%/, "")) +
                        parseFloat(self.$header.css("margin-bottom").replace(/em|px|pt|%/, "")),
                    controlsHeight = self.$panel.find(".layers-selection-panel-controls").outerHeight() +
                        parseFloat(self.$panel.find(".layers-selection-panel-controls").css("margin-bottom").replace(/em|px|pt|%/, "")),
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
        Controls.LayersSelectionPanel.prototype = new Controls.NamedPanel();

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);