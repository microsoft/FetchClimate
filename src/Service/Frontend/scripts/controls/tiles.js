(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.Tile = function (source) {
            var self = this;

            var _state = {};

            var _$tile = typeof source !== "undefined" ? source : $("<div></div>");

            Object.defineProperty(self, "$tile", {
                get: function () {
                    return _$tile;
                }                
            });

            self.appendTo = function ($parent) {
                _$tile.appendTo($parent);
            };
        };
      
        Controls.ResultsTile = function (title, info, inProgress) {
            var self = this;

            self.base = Controls.Tile;
            self.base();

            var _$title = $("<div></div>").addClass("tile-title"),
                _$info = $("<div></div>").addClass("tile-info"),
                _$progress = $("<div></div>").addClass("tile-progress");

            Object.defineProperties(self, {
                "title": {
                    get: function () {
                        return _$title.text();
                    },
                    set: function (value) {
                        _$title.text(value);
                    }
                },
                "info": {
                    get: function () {
                        return _$info.html();
                    },
                    set: function (value) {
                        _$info.html(value);
                    }
                },
                "inProgress": {
                    get: function () {
                        return _$progress.is(":visible");
                    },
                    set: function (value) {
                        if (value) {
                            _$progress.css("display", "inline-block");
                        } else {
                            _$progress.css("display", "none");
                        }
                    }
                }
            });

            function initialize() {
                // Set content.
                self.title = title;
                self.info = info;
                self.inProgress = inProgress;

                self.$tile.addClass("results-tile")
                    .append(_$title)
                    .append(_$info)
                    .append(_$progress);

                self.$tile.click(function (event) {
                    var $this = $(this);
                    $(".results-tile").removeClass("selected");
                    if ($this.hasClass("selected")) {
                        $this.removeClass("selected");
                    } else {
                        $this.addClass("selected");

                        FC.state.selectVariable(self.$tile.attr("data-variable"));
                        $this.trigger("selectedvariablechange");
                    }
                });    
            }

            initialize();
        };
        Controls.ResultsTile.prototype = new Controls.Tile();

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);