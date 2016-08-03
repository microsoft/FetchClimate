// Disable D3 Heatmap Worker to avoid security error.
window.Worker = null;

/**
 * Data provider pattern applied to Jasmine.
 * It allows to DRY up Jasmine tests that needs to be executed with multiple values.
 * NOTE: https://github.com/jphpsf/jasmine-data-provider
 * @param  {string} name   Name for a set of values.
 * @param  {Array} values A set of values to expect.
 * @param  {function} func   function(value) which contains specs.
 */
function using(name, values, func) {
    for (var i = 0, count = values.length; i < count; i++) {
        if (Object.prototype.toString.call(values[i]) !== '[object Array]') {
            values[i] = [values[i]];
        }
        func.apply(this, values[i]);
        jasmine.currentEnv_.currentSpec.description += ' (with "' + name + '" using ' + values[i].join(', ') + ')';
    }
}