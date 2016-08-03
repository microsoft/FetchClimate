(function (D3Ext, $, undefined) {

    D3Ext.ConstLinesPlot = function (jqDiv, master) {

        this.base = D3.CanvasPlot;
        this.base(jqDiv, master);

        var styleData = {};
        D3.Utils.readStyle(jqDiv, styleData);

        var _highlightX, _highlightY;

        var _highlightXColor = typeof(styleData.colorX) == "undefined" ? "gray" : styleData.colorX;
        var _highlightYColor = typeof (styleData.colorY) == "undefined" ? "gray" : styleData.colorY;

        Object.defineProperty(this, "highlightX", { get: function () { return _highlightX; }, set: function (value) { _highlightX = value; this.updateLayout(); }, configurable: false });
        Object.defineProperty(this, "highlightY", { get: function () { return _highlightY; }, set: function (value) { _highlightY = value; this.updateLayout(); }, configurable: false });

        Object.defineProperty(this, "highlightColorX", { get: function () { return _highlightColorX; }, set: function (value) { _highlightColorX = value; this.updateLayout(); }, configurable: false });
        Object.defineProperty(this, "highlightColorY", { get: function () { return _highlightColorY; }, set: function (value) { _highlightColorY = value; this.updateLayout(); }, configurable: false });

        this.renderCore = function (plotRect, screenSize) {
            var context = this.getContext(true);

            if (!context) return;

            var t = this.getTransform();
            var dataToScreenX = t.dataToScreenX;
            var dataToScreenY = t.dataToScreenY;

            var ws = t.dataToScreenX(_highlightX);
            if (ws >= 0 && ws < screenSize.width) {
                context.beginPath();
                context.strokeStyle = _highlightXColor;
                context.moveTo(ws, 0);
                context.lineTo(ws, screenSize.height - 1);
                context.stroke();
            }

            var hs = t.dataToScreenY(_highlightY);
            if (hs >= 0 && hs < screenSize.height) {
                context.beginPath();
                context.strokeStyle = _highlightYColor;
                context.moveTo(0, hs);
                context.lineTo(screenSize.width - 1, hs);
                context.stroke();
            }
        };
    };

    D3Ext.ConstLinesPlot.prototype = new D3.CanvasPlot();
    D3.register('constLines', function (jqDiv, master) { return new D3Ext.ConstLinesPlot(jqDiv, master); });

})(window.D3Ext = window.D3Ext || {}, jQuery);