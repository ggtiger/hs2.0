<template>
  <list-t01
    title="Word模版定义"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M12/A03"
  >
    <rs-modal ref="madd" :width="1200">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="Word模版定义" :ID="CDID" @saved="onSaved"></rsAdd>
    </rs-modal>
    <TableItem title="操作" :width="260" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="editTemplate(data)"
          v-per="'RS_M12/A08'"
        >在线编辑</button>
        <button
          class="h-btn h-btn-s h-btn-green"
          @click.stop="migrateTemplate(data)"
          v-per="'RS_M12/A08'"
        >迁移</button>
        <button
          class="h-btn h-btn-s h-btn-yellow"
          @click.stop="copyTemplate(data)"
          v-per="'RS_M12/A03'"
        >复制</button>
        <button
          v-if="(data.ISUSE+'')!=='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M12/A07'"
        >启用</button>
        <button
          v-if="(data.ISUSE+'')==='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M12/A07'"
        >停用</button>
      </template>
    </TableItem>
    <rs-word-template-editor
      ref="wordEditor"
      :module-code="editorModuleCode"
      :template-id="editorTemplateId"
      :business-type="editorBizType"
      @save="onEditorSave"
    ></rs-word-template-editor>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { httpPost } from '@/components/rs-onlyoffice-shared';
export default {
  name: 's01-m12-main',
  components: {
    rsAdd,
  },
  data() {
    return {
      CDID: '',
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        { title: '系统管理' },
        { title: 'Word模版定义' },
      ],
      editorModuleCode: '',
      editorTemplateId: '',
      editorBizType: 'cert',
    };
  },
  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add':
          this.add(param);
          break;
        default:
          break;
      }
    },
    endisable(row) {
      this.$callAction({
        action: Constants.STORE_NAME + '/endisable',
        param: { item: row },
        successText: '操作成功',
      });
    },
    editTemplate(row) {
      if (!row.FILEID) {
        this.$Notice('请先上传模版文件');
        return;
      }
      this.editorModuleCode = row.MODULECODE || '';
      this.editorTemplateId = row.TEMPLATEID || '';
      this.editorBizType = row.TEMPLATETYPE || 'cert';
      this.$refs.wordEditor.open(row.FILEID);
    },
    migrateTemplate(row) {
      var self = this;
      if (!row.FILEID) {
        self.$Notice('请先上传模版文件');
        return;
      }
      self.$confirm('将模版中的 Bookmark 字段迁移为 Content Control (SDT) 字段。\n\n' +
        '• 迁移后原文件会自动备份（.bak）\n' +
        '• 替换引擎已兼容两种方式，迁移前后均可正常使用\n' +
        '• 迁移后建议打开在线编辑器验证\n\n' +
        '确定要迁移吗？').then(function() {
        var apiUrl = db.getUrl('url');
        var token = self.$store.state['user'].access_token;
        self.$busy();
        httpPost(apiUrl + '/api/word-template/migrate-bookmarks/' + row.FILEID, {}, token).then(function(result) {
          self.$free();
          if (result.success) {
            var msg = '迁移完成！\n' +
              '• 成功迁移: ' + result.migratedCount + ' 个字段\n' +
              '• 跳过: ' + result.skippedCount + ' 个字段\n' +
              '• 迁移前: ' + result.before.bookmarks + ' Bookmark, ' + result.before.contentControls + ' SDT\n' +
              '• 迁移后: ' + result.after.bookmarks + ' Bookmark, ' + result.after.contentControls + ' SDT';
            self.$alert(msg);
          } else {
            self.$error(result.Message || '迁移失败');
          }
        }).catch(function(e) {
          self.$free();
          self.$error('迁移失败: ' + (e.message || '网络错误'));
        });
      }).catch(function() {});
    },
    copyTemplate(row) {
      var self = this;
      self.$confirm('将复制模版「' + row.TEMPLATENAME + '」及其模版文件，生成一条独立的副本记录。\n\n确定要复制吗？').then(function() {
        var apiUrl = db.getUrl('url');
        var token = self.$store.state['user'].access_token;
        self.$busy();
        httpPost(apiUrl + '/api/word-template/copy/' + row.ID, {}, token).then(function(result) {
          self.$free();
          if (result.success) {
            self.$callAction({
              action: Constants.STORE_NAME + '/query',
              successText: '复制成功',
            });
          } else {
            self.$error(result.Message || '复制失败');
          }
        }).catch(function(e) {
          self.$free();
          self.$error('复制失败: ' + (e.message || '网络错误'));
        });
      }).catch(function() {});
    },
    onEditorSave(result) {
      this.$callAction({
        action: Constants.STORE_NAME + '/query',
        successText: '模版已更新',
      });
    },
    onSaved() {
      this.$refs.madd.hide();
    },
  },
};
</script>
