import chart from './echarts/chart';
import RsTableCell from './rs-table/rs-table-cell.vue';
import RsTableEdit from './rs-table/rs-table-edit.vue';
import RsTableList from './rs-table/rs-table-list.vue';
import addPageTitle from './views/addPageTitle.vue';
import tableToolBar from './views/tableToolBar.vue';
import ViewDialog from './views/view-dialog.vue';
import RsFormEdit from './rs-form/rs-form-edit.vue';
import RsMetaForm from './rs-form/rs-meta-form.vue';
import RsMetaField from './rs-form/rs-meta-field.vue';
import RsMetaQueryPanel from './rs-query-panel/rs-meta-query-panel.vue';
import RsMetaQueryPanelField from './rs-query-panel/rs-meta-query-panel-field.vue';
import ViewAddT01 from './views/view-add-t01.vue';
import Divider from './views/divider.vue';
import ToolBar from './views/toolBar.vue';
import RsModal from './rs-modal';
import ListT01 from './rs-template/list-t01.vue';
import ListT02 from './rs-template/list-t02.vue';
import ReportT01 from './rs-template/report-t01.vue';
import RsEditItem from './edit/rs-edit-item.vue';
import RsPrintPdf from './printPdf/index.vue';
import RsOnlyofficePreview from './rs-onlyoffice-preview/index.vue';
import RsWordTemplateEditor from './rs-word-template-editor/index.vue';
import RsUploaderTemplate from './rs-uploader-template/index.vue';
const install = function(Vue, config = {}) {
  if (install.installed) return;
  Vue.component(chart.name, chart);
  Vue.component(RsTableCell.name, RsTableCell);
  Vue.component(RsTableEdit.name, RsTableEdit);
  Vue.component(RsTableList.name, RsTableList);
  Vue.component(addPageTitle.name, addPageTitle);
  Vue.component(tableToolBar.name, tableToolBar);
  Vue.component(ViewDialog.name, ViewDialog);
  Vue.component(RsFormEdit.name, RsFormEdit);
  Vue.component(RsMetaForm.name, RsMetaForm);
  Vue.component(RsMetaField.name, RsMetaField);
  Vue.component(RsMetaQueryPanel.name, RsMetaQueryPanel);
  Vue.component(RsMetaQueryPanelField.name, RsMetaQueryPanelField);
  Vue.component(ViewAddT01.name, ViewAddT01);
  Vue.component(Divider.name, Divider);
  Vue.component(ToolBar.name, ToolBar);
  Vue.component(RsModal.name, RsModal);
  Vue.component(ListT01.name, ListT01);
  Vue.component(ListT02.name, ListT02);
  Vue.component(ReportT01.name, ReportT01);
  Vue.component(RsEditItem.name, RsEditItem);
  Vue.component(RsPrintPdf.name, RsPrintPdf);
  Vue.component(RsOnlyofficePreview.name, RsOnlyofficePreview);
  Vue.component(RsWordTemplateEditor.name, RsWordTemplateEditor);
  Vue.component(RsUploaderTemplate.name, RsUploaderTemplate);
};
if (typeof window !== 'undefined' && window.Vue) {
  install(window.Vue);
}

// 导出组件
export default {
  install,
  chart,
  addPageTitle,
  Divider,
  ToolBar,
  RsEditItem,
  RsPrintPdf,
  RsOnlyofficePreview,
  RsWordTemplateEditor,
  RsUploaderTemplate,
};
