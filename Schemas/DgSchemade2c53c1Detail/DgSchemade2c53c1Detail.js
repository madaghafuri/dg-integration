define("DgSchemade2c53c1Detail", [], function() {
	return {
		entitySchemaName: "DgIntegrationProcessLogDetail",
		details: /**SCHEMA_DETAILS*/{}/**SCHEMA_DETAILS*/,
		diff: /**SCHEMA_DIFF*/[]/**SCHEMA_DIFF*/,
		methods: {
			
			getCopyRecordMenuItem: Terrasoft.emptyFn,
            getDeleteRecordMenuItem: Terrasoft.emptyFn,
			getEditRecordMenuItem: Terrasoft.emptyFn,
            getDataImportMenuItem: Terrasoft.emptyFn,
            getRecordRightsSetupMenuItem: Terrasoft.emptyFn,
			getGridSortMenuItem: Terrasoft.emptyFn,
			sortColumn: Terrasoft.emptyFn,
			addGridDataColumns: function(esq) {
                this.callParent(arguments);
                var seqNo = esq.addColumn("MdrSeqNo");
                seqNo.orderPosition = 0;
                seqNo.orderDirection = Terrasoft.OrderDirection.ASC;
            },

			getAddRecordButtonVisible: function() {
				return false;
			},
			
			editCurrentRecord: Terrasoft.emptyFn,
		}
	};
});
