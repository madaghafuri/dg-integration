define("DgIntegrationLogf48cb7caSection", [], function() {
	return {
		entitySchemaName: "DgIntegrationLog",
		details: /**SCHEMA_DETAILS*/{}/**SCHEMA_DETAILS*/,
		diff: /**SCHEMA_DIFF*/[
			{
				"operation": "merge",
				"name": "SeparateModeAddRecordButton",
				"values": {
					"visible": false
				}
			},
			{
				"operation": "merge",
				"name": "CombinedModeAddRecordButton",
				"values": {
					"visible": false
				}
			},
			{
				"operation": "merge",
				"name": "DataGridActiveRowCopyAction",
				"values": {
					"visible": false
				}
			},
		]/**SCHEMA_DIFF*/,
		methods: {
			/**
			 * Method getSectionActions
			 * Override from base
			 */
			getSectionActions: function() {
				const actionMenuItems = this.callParent(arguments);
				actionMenuItems.each(item => {
					if(item.values.Caption.bindTo === "Resources.Strings.ImportFromFileButtonCaption"){
						item.values.Visible = false;
					}
				});
				
				return actionMenuItems;
			},
			
			initFixedFiltersConfig: function() {
                const fixedFilterConfig = {
                    entitySchema: this.entitySchema,
                    filters: [
                        {
                            name: "PeriodFilter",
                            caption: this.get("Resources.Strings.PeriodFilterCaption"),
                            dataValueType: this.Terrasoft.DataValueType.DATE,
                            startDate: {
                                columnName: "CreatedOn",
                                defValue: this.Terrasoft.startOfWeek(new Date())
                            },
                            dueDate: {
                                columnName: "CreatedOn",
                                defValue: this.Terrasoft.endOfWeek(new Date())
                            }
                        }
                    ]
                };
                this.set("FixedFilterConfig", fixedFilterConfig);
            }
		}
	};
});
