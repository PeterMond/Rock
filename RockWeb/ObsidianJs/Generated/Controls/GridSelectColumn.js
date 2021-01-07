System.register(["../Vendor/Vue/vue.js", "./GridColumn.js"], function (exports_1, context_1) {
    "use strict";
    var vue_js_1, GridColumn_js_1;
    var __moduleName = context_1 && context_1.id;
    function OfType() {
        return vue_js_1.defineComponent({
            name: 'GridSelectColumn',
            components: {
                GridColumn: GridColumn_js_1.default()
            },
            setup: function () {
                var gridContext = vue_js_1.inject('gridContext');
                var rowContext = vue_js_1.inject('rowContext');
                var selectAllRows = gridContext.selectAllRows;
                var isThisRowSelected = gridContext.selectedRowIds[rowContext.rowId];
                var isSelected = vue_js_1.ref(selectAllRows || isThisRowSelected);
                return {
                    gridContext: gridContext,
                    rowContext: rowContext,
                    isSelected: isSelected
                };
            },
            computed: {
                rowId: function () {
                    return this.rowContext.rowId;
                },
                isHeader: function () {
                    return this.rowContext.isHeader;
                }
            },
            watch: {
                'gridContext.selectAllRows': function () {
                    if (!this.isHeader) {
                        this.isSelected = this.gridContext.selectAllRows;
                        this.gridContext.selectedRowIds[this.rowId] = this.isSelected;
                    }
                },
                'gridContext.selectedRowIds': function () {
                    if (!this.isHeader) {
                        this.isSelected = this.gridContext.selectedRowIds[this.rowId];
                    }
                },
                isSelected: function () {
                    if (!this.isHeader) {
                        this.gridContext.selectedRowIds[this.rowId] = this.isSelected;
                    }
                }
            },
            template: "\n<GridColumn class=\"grid-select-field\" align=\"center\">\n    <template #header>\n        <div @click.stop class=\"checkbox\">\n            <label title=\"\">\n                <input type=\"checkbox\" class=\"select-all\" v-model=\"gridContext.selectAllRows\" />\n                <span class=\"label-text\">&nbsp;</span>\n            </label>\n        </div>\n    </template>\n    <template #default>\n        <div @click.stop class=\"checkbox\">\n            <label title=\"\">\n                <input type=\"checkbox\" class=\"select-all\" v-model=\"isSelected\" />\n                <span class=\"label-text\">&nbsp;</span>\n            </label>\n        </div>\n    </template>\n</GridColumn>"
        });
    }
    exports_1("default", OfType);
    return {
        setters: [
            function (vue_js_1_1) {
                vue_js_1 = vue_js_1_1;
            },
            function (GridColumn_js_1_1) {
                GridColumn_js_1 = GridColumn_js_1_1;
            }
        ],
        execute: function () {
        }
    };
});
//# sourceMappingURL=GridSelectColumn.js.map