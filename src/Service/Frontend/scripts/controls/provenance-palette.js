(function (D3Ext, $, undefined) {
    
    D3Ext.ProvenancePaletteControl = function (jqDiv, provInfo) {

        var _provInfo = provInfo;
        Object.defineProperty(this, "provInfo", {
            get: function () { return _provInfo; },
            set: function (value) {
                if (value) {
                    _provInfo = value;
                    renderProvenance();
                }
            },
            configurable: false
        });

        var renderProvenance = function () {
            jqDiv[0].innerHTML = "";
            for (var i = 0; i < _provInfo.length; i++) {
                var div = $("<div style='margin: 3px'></div>").appendTo(jqDiv);

                var canvas = $("<canvas style='margin-right: 5px; vertical-align:center; border: 1px solid black; background-color:" + _provInfo[i].color + "'></canvas>").appendTo(div);
                canvas[0].width = 10;
                canvas[0].height = 10;
                $("<span>" + _provInfo[i].name + "</span>").appendTo(div);
            }
        };
        if (_provInfo) renderProvenance();
    };

})(window.D3Ext = window.D3Ext || {}, jQuery);