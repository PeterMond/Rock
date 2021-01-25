System.register(["../Vendor/Vue/vue.js"], function (exports_1, context_1) {
    "use strict";
    var vue_js_1;
    var __moduleName = context_1 && context_1.id;
    return {
        setters: [
            function (vue_js_1_1) {
                vue_js_1 = vue_js_1_1;
            }
        ],
        execute: function () {
            exports_1("default", vue_js_1.defineComponent({
                name: 'Alert',
                props: {
                    dismissible: {
                        type: Boolean,
                        default: false
                    },
                },
                emits: [
                    'dismiss'
                ],
                methods: {
                    onDismiss: function () {
                        this.$emit('dismiss');
                    }
                },
                template: "<div class=\"alert\">\n    <button v-if=\"dismissible\" type=\"button\" class=\"close\" @click=\"onDismiss\">\n        <span>&times;</span>\n    </button>\n    <slot />\n</div>"
            }));
        }
    };
});
//# sourceMappingURL=Alert.js.map