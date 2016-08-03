(function (D3Ext, $, undefined) {

    D3Ext.AreaPlot = function (div, master) {
        // Initialization (#1)
        var initializer = D3.Utils.getDataSourceFunction(div, D3.readCsv);
        var initialData = initializer(div);

        this.base = D3.CanvasPlot;
        this.base(div, master);

        var _y1;
        var _y2;
        var _x;
        var _fill = '#4169ed';

        // default styles:
        if (initialData) {
            _fill = typeof initialData.fill != "undefined" ? initialData.fill : _fill;
        }

        this.draw = function (data) {
            var y1 = data.y1;
            if (!y1) throw "Data series y1 is undefined";
            var n = y1.length;

            var y2 = data.y2;
            if (!y2) throw "Data series y2 is undefined";
            if (y2.length !== n)
                throw "Data series y1 and y2 have different lengths";

            if (!data.x) {
                data.x = D3.Utils.range(0, n - 1);
            }
            if (n != data.x.length) throw "Data series x and y1,y2 have different lengths";
            _y1 = y1;
            _y2 = y2;
            _x = data.x;

            // styles:
            _fill = typeof data.fill != "undefined" ? data.fill : _fill;

            this.invalidateLocalBounds();

            this.requestNextFrameOrUpdate();
            this.fireAppearanceChanged();
        };

        // Returns a rectangle in the plot plane.
        this.computeLocalBounds = function () {
            var dataToPlotX = this.xDataTransform && this.xDataTransform.dataToPlot;
            var dataToPlotY = this.yDataTransform && this.yDataTransform.dataToPlot;

            var upper = D3.Utils.getBoundingBoxForArrays(_x, _y1, dataToPlotX, dataToPlotY);
            var lower = D3.Utils.getBoundingBoxForArrays(_x, _y2, dataToPlotX, dataToPlotY);

            return D3.Utils.unionRects(upper, lower);
        };

        // Returns 4 margins in the screen coordinate system
        this.getLocalPadding = function () {
            return { left: 0, right: 0, top: 0, bottom: 0 };
        };

        this.renderCore = function (plotRect, screenSize) {
            D3Ext.AreaPlot.prototype.renderCore.call(this, plotRect, screenSize);

            if (_x === undefined || _y1 == undefined || _y2 == undefined)
                return;
            var n = _y1.length;
            if (n == 0) return;

            var t = this.getTransform();
            var dataToScreenX = t.dataToScreenX;
            var dataToScreenY = t.dataToScreenY;

            // size of the canvas
            var w_s = screenSize.width;
            var h_s = screenSize.height;
            var xmin = 0, xmax = w_s;
            var ymin = 0, ymax = h_s;

            var context = this.getContext(true);

            if (!context) return;

            context.fillStyle = _fill;

            //Drawing polygons
            var polygons = [];
            var curInd = undefined;
            for (var i = 0; i < n; i++) {
                if (isNaN(_x[i]) || isNaN(_y1[i]) || isNaN(_y2[i])) {
                    if (curInd === undefined) {
                        curInd = i;
                    }
                    else {
                        polygons.push([curInd, i]);
                        curInd = undefined;
                    }
                } else {
                    if (curInd === undefined) {
                        curInd = i;
                    }
                    else {
                        if (i === n - 1) {
                            polygons.push([curInd, i]);
                            curInd = undefined;
                        }
                    }
                }
            }

            var nPoly = polygons.length;
            for (var i = 0; i < nPoly; i++) {
                context.beginPath();
                var curPoly = polygons[i];
                context.moveTo(dataToScreenX(_x[curPoly[0]]), dataToScreenY(_y1[curPoly[0]]));
                for (var j = curPoly[0] + 1; j <= curPoly[1]; j++) {
                    context.lineTo(dataToScreenX(_x[j]), dataToScreenY(_y1[j]));
                }
                for (var j = curPoly[1]; j >= curPoly[0]; j--) {
                    context.lineTo(dataToScreenX(_x[j]), dataToScreenY(_y2[j]));
                }
                context.fill();
            }
        };

        // Clipping algorithms
        var code = function (x, y, xmin, xmax, ymin, ymax) {
            return (x < xmin) << 3 | (x > xmax) << 2 | (y < ymin) << 1 | (y > ymax);
        };


        // Others
        this.onDataTransformChanged = function (arg) {
            this.invalidateLocalBounds();
            D3Ext.AreaPlot.prototype.onDataTransformChanged.call(this, arg);
        };

        Object.defineProperty(this, "fill", {
            get: function () { return _fill; },
            set: function (value) {
                if (value == _fill) return;
                _fill = value;

                this.fireAppearanceChanged("fill");
                this.requestNextFrameOrUpdate();
            },
            configurable: false
        });

        this.getLegend = function () {
            var div = $("<div class='d3-legend-item'></div>");

            var drawLegendIcon = function (ctx, width, height) {
                ctx.clearRect(0, 0, width, height);

                ctx.beginPath();
                ctx.moveTo(0, 1);
                ctx.lineTo(10, 13);
                ctx.lineTo(20, 1);

                ctx.lineTo(20, 8);
                ctx.lineTo(10, 19);
                ctx.lineTo(0, 8);

                ctx.lineTo(0, 4);
                ctx.fill();
            }

            var canvas = $("<canvas style='margin-right: 15px'></canvas>").appendTo(div);
            canvas[0].width = 20;
            canvas[0].height = 20;
            var ctx = canvas.get(0).getContext("2d");
            ctx.fillStyle = _fill;
            drawLegendIcon(ctx, 20, 20);

            var name = $("<span>" + this.name + "</span>").appendTo(div);

            this.host.bind("appearanceChanged",
                function () {
                    ctx.fillStyle = _fill;
                    drawLegendIcon(ctx, 20, 20);
                });

            var that = this;

            var onLegendRemove = function () {
                that.host.unbind("appearanceChanged");

                div[0].innerHTML = "";
                div.removeClass("d3-legend-item");
            };

            return { div: div, onLegendRemove: onLegendRemove };
        };

        // Initialization 
        if (initialData && typeof initialData.y != 'undefined')
            this.draw(initialData);
    }

    D3Ext.AreaPlot.prototype = new D3.CanvasPlot;

})(window.D3Ext = window.D3Ext || {}, jQuery);