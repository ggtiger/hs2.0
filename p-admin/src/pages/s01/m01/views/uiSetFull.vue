<template>
  <div class="uis-full">
    <!-- 顶栏 -->
    <div class="uis-header">
      <div class="uis-title">
        <span class="uis-title-label">页面配置</span>
        <span class="uis-title-res">{{ resourceName || resourceId }}</span>
      </div>
    </div>

    <div class="uis-body">
      <!-- 左侧 -->
      <div class="uis-left" :style="{ width: leftWidth ? leftWidth + 'px' : '' }">
        <!-- Tab 切换 -->
        <div class="uis-tabs">
          <div
            v-for="t in tabs"
            :key="t.key"
            class="uis-tab"
            :class="{ active: activeTab === t.key }"
            @click="activeTab = t.key"
          >{{ t.title }}</div>
        </div>

        <!-- 字段列表 -->
        <div class="uis-field-list">
          <div class="uis-toolbar">
            <Button size="s" color="primary" icon="h-icon-search" @click="openFieldSel">选入资源字段</Button>
            <Button size="s" icon="h-icon-plus" @click="addBlank">新增</Button>
            <span class="uis-toolbar-tip">勾选字段→显示于该 Tab；拖拽行或↑↓调整顺序</span>
          </div>
          <div class="uis-fields">
            <div
              v-for="(row, idx) in visibleFields"
              :key="rowKey(row, idx)"
              class="uis-field-row"
              :class="{
                active: currentRow === row,
                unchecked: !isChecked(row),
                dragging: dragIndex === idx,
                'drag-over-top': dragOverIndex === idx && dragOverPos === 'top',
                'drag-over-bottom': dragOverIndex === idx && dragOverPos === 'bottom',
              }"
              draggable="true"
              @click="currentRow = row"
              @dragstart="onDragStart(idx, $event)"
              @dragover="onDragOver(idx, $event)"
              @drop="onDrop(idx, $event)"
              @dragleave="onDragLeave(idx)"
              @dragend="onDragEnd"
            >
              <input
                type="checkbox"
                :checked="isChecked(row)"
                @change="onToggleVisible(row)"
                @click.stop
              />
              <div class="uis-field-name">
                <span class="lbl">{{ row.LABELNAME || '(未命名)' }}</span>
                <span class="fld">{{ row.RESFIELDNAME || row.FIELDNAME || '(无字段名)' }}</span>
              </div>
              <div class="uis-field-ops" v-if="isChecked(row)">
                <button class="uis-op-btn" title="上移" @click.stop="moveUp(row)">↑</button>
                <button class="uis-op-btn" title="下移" @click.stop="moveDown(row)">↓</button>
              </div>
              <div class="uis-field-ops" v-else>
                <span class="uis-op-disabled">未启用</span>
              </div>
              <button class="uis-op-btn uis-op-del" title="删除" @click.stop="removeField(row)">✕</button>
            </div>
            <div v-if="visibleFields.length === 0" class="uis-empty">
              暂无字段，请点击"选入资源字段"或"新增"
            </div>
          </div>
        </div>

        <!-- 字段列表↔属性面板 拖拽条 -->
        <div class="uis-resizer-h" @mousedown="onFieldResizeStart"></div>

        <!-- 字段属性面板 -->
        <div class="uis-prop-panel" :style="{ height: propPanelHeight + 'px' }">
          <div class="uis-prop-title">字段属性</div>
          <div v-if="currentRow" class="uis-prop-body">
            <Form :label-width="90" mode="single" :model="currentRow">
              <div class="uis-prop-group">通用</div>
              <FormItem label="标签">
                <input
                  type="text"
                  :value="currentRow.LABELNAME"
                  @input="setField('LABELNAME', $event.target.value)"
                />
              </FormItem>
              <FormItem label="字段名">
                <input
                  type="text"
                  :value="currentRow.FIELDNAME"
                  @input="setField('FIELDNAME', $event.target.value)"
                />
              </FormItem>
              <FormItem label="编辑类型">
                <Select
                  v-model="currentRow.EDITTYPE"
                  :datas="editTypeOptions"
                  keyName="key"
                  titleName="title"
                  @change="setField('EDITTYPE', currentRow.EDITTYPE)"
                ></Select>
              </FormItem>
              <FormItem label="可编辑">
                <input
                  type="checkbox"
                  :checked="+currentRow.EDITABLE === 1"
                  @change="setField('EDITABLE', $event.target.checked ? 1 : 0)"
                />
              </FormItem>
              <FormItem label="允许空">
                <input
                  type="checkbox"
                  :checked="+currentRow.NULLABLE === 1"
                  @change="setField('NULLABLE', $event.target.checked ? 1 : 0)"
                />
              </FormItem>
              <FormItem label="最大长度">
                <input
                  type="number"
                  :value="currentRow.MAXLENGTH"
                  @input="setField('MAXLENGTH', $event.target.value)"
                />
              </FormItem>
              <FormItem label="提示语">
                <input
                  type="text"
                  :value="currentRow.PLACEHOLDER"
                  @input="setField('PLACEHOLDER', $event.target.value)"
                />
              </FormItem>
              <FormItem label="复制配置">
                <Button size="s" icon="h-icon-copy" @click="openCopyConfigPopup">从其他字段复制</Button>
              </FormItem>

              <!-- 列表 Tab 专属 -->
              <template v-if="activeTab === 'list'">
                <div class="uis-prop-group">列表专属</div>
                <FormItem label="显示在列表">
                  <input
                    type="checkbox"
                    :checked="+currentRow.DISPLAYINLIST === 1"
                    @change="setField('DISPLAYINLIST', $event.target.checked ? 1 : 0)"
                  />
                </FormItem>
                <FormItem label="列宽">
                  <input
                    type="text"
                    :value="currentRow.SHOWLENGTH"
                    placeholder="数字(100) 或 >100 或 <100"
                    @input="setField('SHOWLENGTH', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="动作代码" v-if="currentRow.EDITTYPE === 'action'">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.ACTIONCODE"
                    placeholder="点击右侧按钮配置"
                    @click="openActionDialog"
                  />
                </FormItem>
              </template>

              <!-- 表单 Tab 专属 -->
              <template v-if="activeTab === 'form'">
                <div class="uis-prop-group">表单专属</div>
                <FormItem label="表单分组">
                  <input
                    type="text"
                    :value="currentRow.EDITGROUP"
                    @input="setField('EDITGROUP', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="占宽">
                  <Select
                    :value="currentRow.COLSPAN || 1"
                    :datas="colspanOptions"
                    keyName="key"
                    titleName="title"
                    @change="setField('COLSPAN', currentRow.COLSPAN)"
                  ></Select>
                </FormItem>
              </template>

              <!-- 查询 Tab 专属 -->
              <template v-if="activeTab === 'query'">
                <div class="uis-prop-group">查询专属</div>
                <FormItem label="查询类型">
                  <Select
                    v-model="currentRow.QUERYTYPE"
                    :datas="queryTypeOptions"
                    keyName="key"
                    titleName="title"
                    @change="setField('QUERYTYPE', currentRow.QUERYTYPE)"
                  ></Select>
                </FormItem>
                <FormItem label="匹配方式">
                  <Select
                    v-model="currentRow.QUERYMODE"
                    :datas="queryModeOptions"
                    keyName="key"
                    titleName="title"
                    @change="setField('QUERYMODE', currentRow.QUERYMODE)"
                  ></Select>
                </FormItem>
              </template>

              <!-- 下拉数据（select） -->
              <template v-if="currentRow.EDITTYPE === 'select'">
                <div class="uis-prop-group">下拉数据</div>
                <FormItem label="数据字典">
                  <Select
                    :value="getSelectDictName(currentRow.SELECTDATA)"
                    @input="onDictSelect($event)"
                    :datas="dictNameOptions"
                    :filterable="true"
                    placeholder="选择系统字典"
                  ></Select>
                </FormItem>
                <FormItem label="数据范围" v-if="getSelectDictName(currentRow.SELECTDATA)">
                  <Select
                    :value="getSelectDictItems(currentRow.SELECTDATA)"
                    @input="onDictRangeChange($event)"
                    :datas="currentDictItemOptions"
                    keyName="key"
                    titleName="title"
                    :multiple="true"
                    placeholder="留空=全部，选择=仅含选中项"
                  ></Select>
                </FormItem>
                <FormItem label="自定义数据">
                  <input
                    type="text"
                    readonly
                    :value="isSelectDict(currentRow.SELECTDATA) ? '' : currentRow.SELECTDATA"
                    placeholder="点击右侧按钮配置"
                    @click="openCustomDataDialog"
                  />
                  <Button size="s" class="ml5" @click="openCustomDataDialog">配置</Button>
                </FormItem>
              </template>

              <!-- 自动完成专属 -->
              <template v-if="currentRow.EDITTYPE === 'autocomplete'">
                <div class="uis-prop-group">自动完成</div>
                <FormItem label="快捷预设">
                  <Select
                    @input="applySelPreset($event)"
                    :datas="selTypeOptions"
                    keyName="key"
                    titleName="title"
                    placeholder="选预设自动填充"
                  ></Select>
                </FormItem>
                <FormItem label="模块">
                  <Select
                    :value="getSelField('module')"
                    @input="onSelModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="如 RS_M00"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="接口">
                  <Select
                    :value="getSelField('apiCode')"
                    @input="onSelApiChange($event)"
                    :datas="apiListOfModule"
                    keyName="APICODE"
                    titleName="APINAME"
                    placeholder="如 A05"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="值字段">
                  <Select
                    :value="getSelField('keyName')"
                    @input="setSelField('keyName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 ID"
                  ></Select>
                </FormItem>
                <FormItem label="显示字段">
                  <Select
                    :value="getSelField('titleName')"
                    @input="setSelField('titleName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 DEPTNAME"
                  ></Select>
                </FormItem>
                <FormItem label="更新字段">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.UPDATEFIELDS"
                    placeholder="点击右侧按钮配置"
                    @click="openUpdateFieldsDialog"
                  />
                </FormItem>
                <FormItem label="传入字段">
                  <input
                    type="text"
                    readonly
                    :value="getSelField('paramMappings')"
                    placeholder="搜索时传入其他字段值"
                    @click="openParamMappingsDialog"
                  />
                  <Button size="s" class="ml5" @click="openParamMappingsDialog">配置</Button>
                </FormItem>
                <FormItem label="默认参数">
                  <input
                    type="text"
                    :value="defaultParamsText"
                    @input="onDefaultParamsInput($event.target.value)"
                    placeholder='JSON 如 {"TYPE":"1"}'
                  />
                </FormItem>
              </template>

              <!-- 多选自动完成专属：同 autocomplete 数据源 + 绑定模式 -->
              <template v-if="currentRow.EDITTYPE === 'multiautocomplete'">
                <div class="uis-prop-group">多选自动完成</div>
                <FormItem label="快捷预设">
                  <Select
                    @input="applySelPreset($event)"
                    :datas="selTypeOptions"
                    keyName="key"
                    titleName="title"
                    placeholder="选预设自动填充"
                  ></Select>
                </FormItem>
                <FormItem label="模块">
                  <Select
                    :value="getSelField('module')"
                    @input="onSelModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="如 RS_M00"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="接口">
                  <Select
                    :value="getSelField('apiCode')"
                    @input="onSelApiChange($event)"
                    :datas="apiListOfModule"
                    keyName="APICODE"
                    titleName="APINAME"
                    placeholder="如 A05"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="值字段">
                  <Select
                    :value="getSelField('keyName')"
                    @input="setSelField('keyName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 ID"
                  ></Select>
                </FormItem>
                <FormItem label="显示字段">
                  <Select
                    :value="getSelField('titleName')"
                    @input="setSelField('titleName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 DEPTNAME"
                  ></Select>
                </FormItem>
                <FormItem label="绑定模式">
                  <Select
                    :value="getSelField('mode') || 'subtable'"
                    @input="setSelField('mode', $event)"
                    :datas="multSelModeOptions"
                    keyName="key"
                    titleName="title"
                  ></Select>
                </FormItem>
                <FormItem label="目标模块" v-if="(getSelField('mode') || 'subtable') === 'subtable'">
                  <Select
                    :value="getSelField('targetModule')"
                    @input="onTargetModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="子表所属模块（可与来源不同）"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="子表数据源" v-if="(getSelField('mode') || 'subtable') === 'subtable'">
                  <Select
                    :value="getSelField('subtable')"
                    @input="onSubtableChange($event)"
                    :datas="subtableOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="选择模块的子表数据源"
                  ></Select>
                </FormItem>
                <FormItem label="映射字段" v-if="(getSelField('mode') || 'subtable') === 'subtable'">
                  <input
                    type="text"
                    readonly
                    :value="getSelField('subMappings')"
                    placeholder="点击右侧按钮配置 子表字段,远程字段"
                    @click="openSubMappingsDialog"
                  />
                  <Button size="s" class="ml5" @click="openSubMappingsDialog">配置</Button>
                </FormItem>
                <FormItem label="目标字段" v-if="getSelField('mode') === 'field'">
                  <input
                    type="text"
                    :value="getSelField('field')"
                    @input="setSelField('field', $event.target.value)"
                    placeholder="存逗号id 的字段名（默认=字段名）"
                  />
                </FormItem>
              </template>

              <!-- 树形选择专属 -->
              <template v-if="currentRow.EDITTYPE === 'treepicker'">
                <div class="uis-prop-group">树形选择</div>
                <FormItem label="快捷预设">
                  <Select
                    @input="applySelPreset($event)"
                    :datas="treeSelTypeOptions"
                    keyName="key"
                    titleName="title"
                    placeholder="选预设自动填充"
                  ></Select>
                </FormItem>
                <FormItem label="模块">
                  <Select
                    :value="getSelField('module')"
                    @input="onSelModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="如 RS_M00"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="接口">
                  <Select
                    :value="getSelField('apiCode')"
                    @input="onSelApiChange($event)"
                    :datas="apiListOfModule"
                    keyName="APICODE"
                    titleName="APINAME"
                    placeholder="如 A04"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="值字段">
                  <Select
                    :value="getSelField('keyName')"
                    @input="setSelField('keyName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 ID"
                  ></Select>
                </FormItem>
                <FormItem label="显示字段">
                  <Select
                    :value="getSelField('titleName')"
                    @input="setSelField('titleName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 DEPTNAME"
                  ></Select>
                </FormItem>
                <FormItem label="父字段">
                  <Select
                    :value="getSelField('parentName')"
                    @input="setSelField('parentName', $event)"
                    :datas="fieldOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 UPDEPTID"
                  ></Select>
                </FormItem>
                <FormItem label="更新字段">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.UPDATEFIELDS"
                    placeholder="点击右侧按钮配置"
                    @click="openUpdateFieldsDialog"
                  />
                  <Button size="s" class="ml5" @click="openUpdateFieldsDialog">配置</Button>
                </FormItem>
                <FormItem label="传入字段">
                  <input
                    type="text"
                    readonly
                    :value="getSelField('paramMappings')"
                    placeholder="搜索时传入其他字段值"
                    @click="openParamMappingsDialog"
                  />
                  <Button size="s" class="ml5" @click="openParamMappingsDialog">配置</Button>
                </FormItem>
                <FormItem label="默认参数">
                  <input
                    type="text"
                    :value="defaultParamsText"
                    @input="onDefaultParamsInput($event.target.value)"
                    placeholder='JSON 如 {"TYPE":"1"}'
                  />
                </FormItem>
              </template>

              <!-- 上传专属（文件/图片） -->
              <template v-if="currentRow.EDITTYPE === 'fileupload' || currentRow.EDITTYPE === 'imageupload'">
                <div class="uis-prop-group">上传配置</div>
                <FormItem label="文件大小">
                  <input
                    type="text"
                    :value="uploaderProp('maxFileSize')"
                    placeholder="如 10mb / 1mb"
                    @input="setUploaderProp('maxFileSize', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="允许类型" v-if="currentRow.EDITTYPE === 'fileupload'">
                  <input
                    type="text"
                    :value="uploaderProp('accept')"
                    placeholder="如 .docx,.pdf"
                    @input="setUploaderProp('accept', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="多文件">
                  <h-switch
                    :value="!!uploaderProp('multifile')"
                    @input="onUploaderMultiFileChange($event)"
                  >
                    <span slot="open">是</span>
                    <span slot="close">否</span>
                  </h-switch>
                  <span style="margin-left:8px;color:#999;font-size:12px;">逗号id存单字段</span>
                </FormItem>
                <FormItem label="绑定子表">
                  <h-switch
                    :value="uploaderProp('mode') === 'subtable'"
                    @input="onUploaderSubtableChange($event)"
                  >
                    <span slot="open">是</span>
                    <span slot="close">否</span>
                  </h-switch>
                  <span style="margin-left:8px;color:#999;font-size:12px;">每文件=子表一行</span>
                </FormItem>
                <FormItem label="目标模块" v-if="uploaderProp('mode') === 'subtable'">
                  <Select
                    :value="uploaderProp('targetModule')"
                    @input="onUploadTargetModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="子表所属模块"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="子表名" v-if="uploaderProp('mode') === 'subtable'">
                  <Select
                    :value="uploaderProp('subtable')"
                    @input="onUploadSubtableChange($event)"
                    :datas="subtableOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="选择子表数据源"
                  ></Select>
                </FormItem>
                <FormItem label="映射字段" v-if="uploaderProp('mode') === 'subtable'">
                  <input
                    type="text"
                    readonly
                    :value="uploaderProp('subMappings')"
                    placeholder="子表字段,上传字段;..."
                    @click="openUploadMappingsDialog"
                  />
                  <Button size="s" class="ml5" @click="openUploadMappingsDialog">配置</Button>
                </FormItem>
                <FormItem label="更新字段" v-if="!uploaderProp('multifile') && uploaderProp('mode') !== 'subtable'">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.UPDATEFIELDS"
                    placeholder="如 FILEID,id;FILENAME,name"
                    @click="openUpdateFieldsDialog"
                  />
                  <Button size="s" class="ml5" @click="openUpdateFieldsDialog">配置</Button>
                </FormItem>
              </template>

              <!-- 文件上传+模板选择专属 -->
              <template v-if="currentRow.EDITTYPE === 'fileuploadtpl'">
                <div class="uis-prop-group">上传模板配置</div>
                <FormItem label="模板类型">
                  <input
                    type="text"
                    :value="uploaderTplProp('templateType')"
                    placeholder="如 YSJL（原始记录模板）"
                    @input="setUploaderTplProp('templateType', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="模块编码">
                  <input
                    type="text"
                    :value="uploaderTplProp('moduleCode')"
                    placeholder="模板所属模块（留空=当前模块）"
                    @input="setUploaderTplProp('moduleCode', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="文件大小">
                  <input
                    type="text"
                    :value="uploaderTplProp('maxFileSize')"
                    placeholder="如 10mb / 1mb"
                    @input="setUploaderTplProp('maxFileSize', $event.target.value)"
                  />
                </FormItem>
                <FormItem label="多文件">
                  <h-switch
                    :value="!!uploaderTplProp('multifile')"
                    @input="setUploaderTplProp('multifile', $event)"
                  >
                    <span slot="open">是</span>
                    <span slot="close">否</span>
                  </h-switch>
                  <span style="margin-left:8px;color:#999;font-size:12px;">逗号id存单字段</span>
                </FormItem>
                <FormItem label="显示选入">
                  <h-switch
                    :value="uploaderTplProp('showSelect') !== false"
                    @input="setUploaderTplProp('showSelect', $event)"
                  >
                    <span slot="open">是</span>
                    <span slot="close">否</span>
                  </h-switch>
                  <span style="margin-left:8px;color:#999;font-size:12px;">显示"选入模版"按钮</span>
                </FormItem>
                <FormItem label="更新字段" v-if="!uploaderTplProp('multifile')">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.UPDATEFIELDS"
                    placeholder="如 FILEID,id;FILENAME,name"
                    @click="openUpdateFieldsDialog"
                  />
                  <Button size="s" class="ml5" @click="openUpdateFieldsDialog">配置</Button>
                </FormItem>
              </template>

              <!-- 代码专属 -->
              <template v-if="currentRow.EDITTYPE === 'code'">
                <div class="uis-prop-group">代码配置</div>
                <FormItem label="代码语言">
                  <Select
                    v-model="codeLanguage"
                    :datas="codeLangOptions"
                    keyName="key"
                    titleName="title"
                    @change="onCodeLangChange"
                  ></Select>
                </FormItem>
              </template>

              <!-- 表格区块专属：分组标题 + 可编辑子表 -->
              <template v-if="currentRow.EDITTYPE === 'tableblock'">
                <div class="uis-prop-group">表格区块</div>
                <FormItem label="目标模块">
                  <Select
                    :value="getSelField('targetModule')"
                    @input="onTableBlockModuleChange($event)"
                    :datas="moduleList"
                    keyName="MODULECODE"
                    titleName="MODULENAME"
                    placeholder="子表所属模块"
                    :filterable="true"
                  ></Select>
                </FormItem>
                <FormItem label="子表数据源">
                  <Select
                    :value="getSelField('subtable')"
                    @input="setSelField('subtable', $event)"
                    :datas="subtableOptions"
                    keyName="key"
                    titleName="title"
                    :filterable="true"
                    placeholder="如 DTSB（默认=字段名）"
                  ></Select>
                </FormItem>
                <FormItem label="显示按钮">
                  <div style="display:flex;flex-wrap:wrap;gap:8px 12px;">
                    <Checkbox v-model="tableBlockBtnAdd">新增</Checkbox>
                    <Checkbox v-model="tableBlockBtnRemove">移除</Checkbox>
                    <Checkbox v-model="tableBlockBtnUp">上移</Checkbox>
                    <Checkbox v-model="tableBlockBtnDown">下移</Checkbox>
                  </div>
                </FormItem>
                <FormItem label="自定义按钮">
                  <input
                    type="text"
                    readonly
                    :value="tbButtonsSummary"
                    placeholder="点击右侧按钮配置自定义按钮"
                    @click="openTbButtonsDialog"
                  />
                  <Button size="s" class="ml5" @click="openTbButtonsDialog">配置</Button>
                </FormItem>
              </template>

              <!-- 页面按钮专属：列表页顶部按钮，走 ACTIONCODE 标准规则 -->
              <template v-if="currentRow.EDITTYPE === 'pageaction'">
                <div class="uis-prop-group">页面按钮</div>
                <FormItem label="动作代码">
                  <input
                    type="text"
                    readonly
                    :value="currentRow.ACTIONCODE"
                    placeholder="点击右侧按钮配置按钮"
                    @click="openActionDialog"
                  />
                  <Button size="s" class="ml5" @click="openActionDialog">配置</Button>
                </FormItem>
              </template>

              <!-- 权限与显隐（通用） -->
              <template v-if="currentRow.EDITTYPE && currentRow.EDITTYPE !== 'toolbar'">
                <div class="uis-prop-group">权限与显隐</div>
                <FormItem label="显隐条件">
                  <input
                    type="text"
                    :value="currentRow.VISIBLEIF"
                    @input="setField('VISIBLEIF', $event.target.value)"
                    placeholder="IS 开头的 computed 名，如 ISREFTYPE"
                  />
                </FormItem>
                <FormItem label="功能模块">
                  <Select
                    :value="perFuncCode"
                    @input="onPerFuncChange($event)"
                    :datas="actionFuncOptions"
                    keyName="FUNCCODE"
                    titleName="FUNCNAME"
                    :filterable="true"
                    placeholder="选功能模块"
                  ></Select>
                </FormItem>
                <FormItem label="功能点">
                  <Select
                    :value="perPointCode"
                    @input="onPerPointChange($event)"
                    :datas="perPointOptions"
                    keyName="FUNCPOINTCODE"
                    titleName="FUNCPOINTNAME"
                    :filterable="true"
                    placeholder="选功能点（权限码）"
                  ></Select>
                </FormItem>
              </template>

              <!-- 高级配置：对所有类型都显示，方便查看/清除 SELECTDATA -->
              <template v-if="!['select','autocomplete','multiautocomplete','treepicker','fileupload','imageupload','fileuploadtpl','code','tableblock'].includes(currentRow.EDITTYPE) && currentRow.SELECTDATA">
                <div class="uis-prop-group">高级配置</div>
                <FormItem label="下拉数据">
                  <div class="uis-advanced-sd">
                    <input
                      type="text"
                      :value="currentRow.SELECTDATA"
                      readonly
                    />
                    <Button size="s" color="red" @click="setField('SELECTDATA', '')">清除</Button>
                  </div>
                </FormItem>
              </template>
            </Form>
          </div>
          <div v-else class="uis-empty">点击上方字段以编辑属性</div>
        </div>
      </div>

      <!-- 左右分栏 拖拽条 -->
      <div class="uis-resizer-v" @mousedown="onLeftResizeStart"></div>

      <!-- 右侧预览 -->
      <div class="uis-right">
        <div class="uis-preview-title">
          实时预览（{{ tabTitle }}）
          <span class="uis-preview-tip">{{ previewTip }}</span>
        </div>
        <div class="uis-preview-body">
          <!-- 列表预览：复用 rs-table-list，保证与运行时一致 -->
          <div v-if="activeTab === 'list'">
            <rs-table-list
              :columnConfig="previewColumns"
              :datas="mockRows"
              border
            />
            <div v-if="previewColumns.length === 0" class="uis-empty">暂无列表字段，请在左侧勾选</div>
          </div>

          <!-- 表单画布：分组胶囊 + 字段单元格拖拽 -->
          <div v-else-if="activeTab === 'form'" class="uis-form-canvas">
            <div class="uis-form-canvas-header">
              <div class="uis-form-groups">
                <div
                  v-for="g in formGroups"
                  :key="g.name"
                  class="uis-form-group-tab"
                  :class="{ active: activeFormGroup === g.name }"
                  @click="activeFormGroup = g.name"
                >
                  <span class="uis-form-group-name">{{ g.name }}</span>
                  <span class="uis-form-group-count">{{ g.items.length }}</span>
                </div>
                <div v-if="formGroups.length === 0" class="uis-form-groups-empty">暂无分组</div>
              </div>
              <div class="uis-group-ops">
                <Button size="s" icon="h-icon-plus" @click="openGroupEdit('add')" title="新增分组"></Button>
                <Button size="s" icon="h-icon-edit" @click="openGroupEdit('rename')" title="重命名当前分组"></Button>
                <Button size="s" icon="h-icon-trash" @click="removeCurrentGroup" title="删除当前分组"></Button>
              </div>
            </div>

            <!-- 分组管理弹窗 -->
            <Modal v-model="groupEdit.show" :title="groupEdit.mode === 'add' ? '新增分组' : '重命名分组'" hasCloseIcon middle>
              <div style="padding: 10px;">
                <FormItem label="分组名称">
                  <input type="text" v-model="groupEdit.value" class="h-input" placeholder="请输入分组名称" />
                </FormItem>
              </div>
              <div slot="footer">
                <Button @click="groupEdit.show = false">取消</Button>
                <Button color="primary" class="ml5" @click="saveGroupEdit">确定</Button>
              </div>
            </Modal>

            <!-- 表单设计器画布：复用 rs-form-edit 渲染真实表单 + designer 模式加拖拽 -->
            <div class="uis-form-designer" v-if="currentFormGroup && currentFormGroup.items.length">
              <rs-form-edit
                :fields="currentFormGroupPreviewFields"
                :path="previewFormPath"
                :disabled="false"
                mode="twocolumn"
                :label-width="100"
                designer
                :designer-active-key="currentRow ? (currentRow.RESFIELDNAME || currentRow.FIELDNAME) : ''"
                :designer-drag-key="formDrag.key"
                :designer-drag-over="{ key: formDrag.overKey, pos: formDrag.overPos }"
                @designer-cell-click="onDesignerCellClick"
                @designer-cell-dragstart="onDesignerCellDragStart"
                @designer-cell-dragover="onDesignerCellDragOver"
                @designer-cell-drop="onDesignerCellDrop"
                @designer-cell-dragleave="onDesignerCellDragLeave"
                @designer-cell-dragend="onDesignerCellDragEnd"
              >
                <template #designer-tools="{ field }">
                  <div class="uis-form-designer-tools">
                    <span class="uis-form-designer-handle" title="拖拽排序">⋮⋮</span>
                    <button
                      class="uis-form-designer-tool-btn"
                      :class="{ active: +fieldColspan(field) < 2 }"
                      title="半行"
                      @click.stop="setFieldColspan(field, 1)"
                    >½</button>
                    <button
                      class="uis-form-designer-tool-btn"
                      :class="{ active: +fieldColspan(field) >= 2 }"
                      title="整行"
                      @click.stop="setFieldColspan(field, 2)"
                    >Ⅰ</button>
                  </div>
                  <div class="uis-form-designer-field-tag">{{ field.props && field.props.key }}</div>
                </template>
              </rs-form-edit>
            </div>
            <div v-else class="uis-form-designer-empty">
              <div class="uis-form-designer-empty-inner">
                <div class="uis-form-designer-empty-icon">▢</div>
                <div class="uis-form-designer-empty-text">暂无表单字段</div>
                <div class="uis-form-designer-empty-tip">请在左侧字段列表勾选启用</div>
              </div>
            </div>
          </div>

          <!-- 查询预览 -->
          <div v-else-if="activeTab === 'query'">
            <Form :model="mockQueryModel" mode="twocolumn" :label-width="100">
              <FormItem
                v-for="row in visibleFields"
                :key="'q-' + rowKey(row, 0)"
                v-if="isChecked(row)"
                :label="row.LABELNAME"
                class="uis-query-drag-cell"
                :class="{
                  'query-dragging': queryDrag.key === (row.RESFIELDNAME || row.FIELDNAME),
                  'query-drag-over-before': queryDrag.overKey === (row.RESFIELDNAME || row.FIELDNAME) && queryDrag.overPos === 'before',
                  'query-drag-over-after': queryDrag.overKey === (row.RESFIELDNAME || row.FIELDNAME) && queryDrag.overPos === 'after',
                }"
                draggable="true"
                @dragstart.native="onQueryDragStart(row, $event)"
                @dragover.native="onQueryDragOver(row, $event)"
                @drop.native="onQueryDrop(row, $event)"
                @dragleave.native="onQueryDragLeave(row)"
                @dragend.native="onQueryDragEnd"
              >
                <input
                  v-if="(row.QUERYTYPE || 'input') === 'input'"
                  type="text"
                  v-model="mockQueryModel[row.RESFIELDNAME || row.FIELDNAME]"
                  class="h-input"
                />
                <DateRangePicker
                  v-else-if="row.QUERYTYPE === 'daterange'"
                  v-model="mockQueryModel[row.RESFIELDNAME || row.FIELDNAME]"
                ></DateRangePicker>
                <Select
                  v-else
                  :datas="parseSelectDatasFromText(row.SELECTDATA)"
                  keyName="key"
                  titleName="title"
                  v-model="mockQueryModel[row.RESFIELDNAME || row.FIELDNAME]"
                ></Select>
              </FormItem>
            </Form>
            <div v-if="visibleFields.filter(isChecked).length === 0" class="uis-empty">暂无查询字段，请在左侧勾选</div>
            <div class="uis-preview-query-btns">
              <Button size="s" color="primary">查询</Button>
              <Button size="s" class="ml5">重置</Button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 自定义下拉数据弹窗 -->
    <Modal v-model="customDataShow" title="自定义下拉数据" :width="560" hasCloseIcon middle>
      <div v-if="customDataShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          每行一个选项：键(key) + 值(显示文本)。如 key=1, 值=是
        </div>
        <div
          v-for="(row, idx) in customDataRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <input
            type="text"
            v-model="row.key"
            placeholder="键(key)"
            style="flex:1;padding:4px 8px;border:1px solid #d9d9d9;border-radius:3px;font-size:13px;"
          />
          <span style="color:#999;">:</span>
          <input
            type="text"
            v-model="row.title"
            placeholder="显示文本"
            style="flex:1;padding:4px 8px;border:1px solid #d9d9d9;border-radius:3px;font-size:13px;"
          />
          <Button size="s" icon="h-icon-trash" @click="customDataRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="customDataRows.push({ key: '', title: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="customDataShow = false">取消</Button>
        <Button color="primary" @click="saveCustomDataDialog">确定</Button>
      </div>
    </Modal>

    <!-- 从其他字段复制配置弹窗 -->
    <config-sel-popup ref="configSelPopup" @confirm="onConfigSelConfirm" />

    <!-- 选入资源字段弹窗 -->
    <Modal v-model="fieldSelShow" title="选入资源字段" :width="800" hasCloseIcon middle>
      <fieldSel
        v-if="fieldSelShow"
        :item="resItemForSel"
        @on-select="onSelectFields"
      ></fieldSel>
    </Modal>

    <!-- 更新字段配置弹窗 -->
    <Modal v-model="updateFieldsShow" title="更新字段配置" :width="640" hasCloseIcon middle>
      <div v-if="updateFieldsShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          格式：<code>本地字段,远程字段</code>；多对用分号分隔。本地字段来自当前资源，远程字段来自所选接口。
        </div>
        <div
          v-for="(row, idx) in updateFieldsRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <Select
            v-model="row.local"
            :datas="localFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="本地字段"
            style="flex:1;"
          ></Select>
          <span style="color:#999;">,</span>
          <Select
            v-model="row.remote"
            :datas="remoteFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="远程字段"
            style="flex:1;"
          ></Select>
          <Button size="s" icon="h-icon-trash" @click="updateFieldsRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="updateFieldsRows.push({ local: '', remote: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="updateFieldsShow = false">取消</Button>
        <Button color="primary" @click="saveUpdateFieldsDialog">确定</Button>
      </div>
    </Modal>

    <!-- 传入字段配置弹窗（autocomplete/multiautocomplete 联动传参） -->
    <Modal v-model="paramMappingsShow" title="传入字段配置" :width="640" hasCloseIcon middle>
      <div v-if="paramMappingsShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          格式：<code>本表单字段,接口参数名</code>；多对用分号分隔。搜索时把表单字段值作为参数传给接口。
        </div>
        <div
          v-for="(row, idx) in paramMappingsRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <Select
            v-model="row.local"
            :datas="localFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="本表单字段"
            style="flex:1;"
          ></Select>
          <span style="color:#999;">,</span>
          <input
            type="text"
            v-model="row.remote"
            placeholder="接口参数名（如 CUSTID）"
            style="flex:1;padding:4px 8px;border:1px solid #d9d9d9;border-radius:3px;font-size:13px;"
          />
          <Button size="s" icon="h-icon-trash" @click="paramMappingsRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="paramMappingsRows.push({ local: '', remote: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="paramMappingsShow = false">取消</Button>
        <Button color="primary" @click="saveParamMappingsDialog">确定</Button>
      </div>
    </Modal>

    <!-- 上传子表映射字段配置弹窗 -->
    <Modal v-model="uploadMappingsShow" title="上传子表映射字段配置" :width="640" hasCloseIcon middle>
      <div v-if="uploadMappingsShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          格式：<code>子表字段,上传字段</code>；多对用分号分隔。上传字段如 id/name，子表字段如 FILEID/FILENAME。
        </div>
        <div
          v-for="(row, idx) in uploadMappingsRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <Select
            v-model="row.sub"
            :datas="uploadSubtableFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="子表字段"
            style="flex:1;"
          ></Select>
          <span style="color:#999;">,</span>
          <Select
            v-model="row.remote"
            :datas="uploadFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="上传字段"
            style="flex:1;"
          ></Select>
          <Button size="s" icon="h-icon-trash" @click="uploadMappingsRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="uploadMappingsRows.push({ sub: '', remote: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="uploadMappingsShow = false">取消</Button>
        <Button color="primary" @click="saveUploadMappingsDialog">确定</Button>
      </div>
    </Modal>

    <!-- 子表映射字段配置弹窗（multiautocomplete subtable 模式） -->
    <Modal v-model="subMappingsShow" title="子表映射字段配置" :width="640" hasCloseIcon middle>
      <div v-if="subMappingsShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          格式：<code>子表字段,远程字段</code>；多对用分号分隔。子表字段来自所选子表数据源，远程字段来自所选接口。
        </div>
        <div
          v-for="(row, idx) in subMappingsRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <Select
            v-model="row.sub"
            :datas="subtableFieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="子表字段"
            style="flex:1;"
          ></Select>
          <span style="color:#999;">,</span>
          <Select
            v-model="row.remote"
            :datas="fieldOptions"
            keyName="key"
            titleName="title"
            :filterable="true"
            placeholder="远程字段"
            style="flex:1;"
          ></Select>
          <Button size="s" icon="h-icon-trash" @click="subMappingsRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="subMappingsRows.push({ sub: '', remote: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="subMappingsShow = false">取消</Button>
        <Button color="primary" @click="saveSubMappingsDialog">确定</Button>
      </div>
    </Modal>

    <!-- tableblock 自定义按钮配置弹窗 -->
    <Modal v-model="tbButtonsShow" title="自定义按钮配置" :width="720" hasCloseIcon middle>
      <div v-if="tbButtonsShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          actionCode 格式：<code>标签:功能点编码,模块编码/功能点编码</code>（点击走标准规则）；per 权限码如 RS_M01/A03
        </div>
        <div
          v-for="(row, idx) in tbButtonsRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <input
            type="text"
            v-model="row.label"
            placeholder="按钮标签"
            style="width:100px;flex-shrink:0;"
          />
          <input
            type="text"
            v-model="row.code"
            placeholder="按钮code"
            style="width:100px;flex-shrink:0;"
          />
          <input
            type="text"
            v-model="row.actionCode"
            placeholder="actionCode（标准规则）"
            style="flex:1;"
          />
          <input
            type="text"
            v-model="row.per"
            placeholder="权限码 per"
            style="width:120px;flex-shrink:0;"
          />
          <Button size="s" icon="h-icon-trash" @click="tbButtonsRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="tbButtonsRows.push({ label: '', code: '', actionCode: '', per: '' })">新增一行</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="tbButtonsShow = false">取消</Button>
        <Button color="primary" @click="saveTbButtonsDialog">确定</Button>
      </div>
    </Modal>

    <!-- 动作代码配置弹窗 -->
    <Modal v-model="actionDialogShow" title="动作代码配置" :width="700" hasCloseIcon middle>
      <div v-if="actionDialogShow" style="padding:10px;">
        <div style="margin-bottom:8px;color:#666;font-size:12px;">
          格式：<code>标签:功能点编码,模块编码/功能点编码</code>，多项用 <code>|</code> 分隔。
        </div>
        <div
          v-for="(row, idx) in actionRows"
          :key="idx"
          style="display:flex;align-items:center;gap:8px;margin-bottom:8px;"
        >
          <input
            type="text"
            v-model="row.label"
            placeholder="按钮标签"
            style="width:90px;flex-shrink:0;"
          />
          <input
            type="text"
            v-model="row.pointCode"
            placeholder="功能点编码"
            style="width:100px;flex-shrink:0;"
          />
          <Select
            :value="row.funcCode"
            @input="onActionFuncChange(row, $event)"
            :datas="actionFuncOptions"
            keyName="FUNCCODE"
            titleName="FUNCNAME"
            :filterable="true"
            placeholder="选择功能模块"
            style="flex:1;"
          ></Select>
          <Select
            v-model="row.funcPointCode"
            :datas="row.actionPointList"
            keyName="FUNCPOINTCODE"
            titleName="FUNCPOINTNAME"
            :filterable="true"
            placeholder="选择功能点"
            style="flex:1;"
          ></Select>
          <Button size="s" icon="h-icon-trash" @click="actionRows.splice(idx, 1)">删</Button>
        </div>
        <div style="text-align:left;">
          <Button size="s" icon="h-icon-plus" @click="addActionRow">新增一项</Button>
        </div>
      </div>
      <div slot="footer">
        <Button @click="actionDialogShow = false">取消</Button>
        <Button color="primary" @click="saveActionDialog">确定</Button>
      </div>
    </Modal>
  </div>
</template>

<script>
import { mapDateTable, Constants } from '../store';
import fieldSel from './fieldSel.vue';
import Gen from '@/utils/gen';
import RsFormEdit from '@/components/rs-form/rs-form-edit';
import {
  SEL_TYPES,
  TREE_SEL_TYPES,
  queryModules,
  queryApis,
  queryFieldsByResourceId,
} from '@/utils/selRegistry';
import { EDIT_TYPE_OPTIONS, QUERY_TYPE_OPTIONS, QUERY_MODE_OPTIONS } from '@/constants';
import configSelPopup from '@/pages/s01/m18/views/components/config-sel-popup.vue';

export default {
  name: 's01-m01-uiSet-full',
  components: { fieldSel, RsFormEdit, configSelPopup },
  provide() {
    return {
      validField: () => {},
      removeProp: () => {},
      requireds: [],
      setConfig: () => {},
      updateProp: () => {},
      updateErrorMessage: () => {},
      labelWidth: 80,
      params: { mode: 'single' },
    };
  },
  props: {
    resourceId: { type: String, default: '' },
    resourceName: { type: String, default: '' },
  },
  data() {
    return {
      activeTab: 'list',
      currentRow: null,
      dragIndex: -1,
      dragOverIndex: -1,
      dragOverPos: '',
      // 表单画布拖拽（用 field key 标识，支持 rs-form-edit designer 模式）
      formDrag: {
        key: '',
        overKey: '',
        overPos: '',
      },
      // 查询预览拖拽
      queryDrag: {
        key: '',
        overKey: '',
        overPos: '',
      },
      // 表单画布当前激活分组
      activeFormGroup: '',
      // 用户自定义的空分组名（还没有字段的分组，DTSC 里不存在）
      customGroups: [],
      // 分组管理
      groupEdit: {
        show: false,
        mode: 'add', // add | rename
        value: '',
      },
      saving: false,
      // 自定义下拉数据弹窗
      customDataShow: false,
      customDataRows: [],
      fieldSelShow: false,
      editTypeOptions: EDIT_TYPE_OPTIONS,
      queryTypeOptions: QUERY_TYPE_OPTIONS,
      queryModeOptions: QUERY_MODE_OPTIONS,
      selTypeOptions: SEL_TYPES,
      treeSelTypeOptions: TREE_SEL_TYPES,
      multSelModeOptions: [
        { key: 'subtable', title: '子表（选中项→子表行）' },
        { key: 'field', title: '字段（选中项→逗号id）' },
      ],
      moduleList: [],
      apiListOfModule: [],
      fieldOptions: [],
      mockFormModel: {},
      mockQueryModel: {},
      // UPDATEFIELDS 弹窗
      updateFieldsShow: false,
      updateFieldsRows: [],
      // 子表映射弹窗（multiautocomplete subtable 模式）
      subMappingsShow: false,
      subMappingsRows: [],
      subtableOptions: [],
      subtableFieldOptions: [],
      // 传入字段弹窗（autocomplete/multiautocomplete 联动传参）
      paramMappingsShow: false,
      paramMappingsRows: [],
      // 上传子表映射弹窗
      uploadMappingsShow: false,
      uploadMappingsRows: [],
      // 上传子表专属字段选项（独立于 multiautocomplete 的 subtableFieldOptions）
      uploadSubtableFieldOptions: [],
      uploadFieldOptions: [
        { key: 'id', title: 'id（文件ID）' },
        { key: 'name', title: 'name（文件名）' },
        { key: 'url', title: 'url（文件地址）' },
        { key: 'thumbUrl', title: 'thumbUrl（缩略图）' },
        { key: 'type', title: 'type（文件类型）' },
        { key: 'status', title: 'status（状态）' },
      ],
      // tableblock 自定义按钮弹窗
      tbButtonsShow: false,
      tbButtonsRows: [],
      leftWidth: 0,
      propPanelHeight: 200,
      // 动作代码弹窗
      actionDialogShow: false,
      actionRows: [],
      // 代码编辑类型
      codeLangOptions: [
        { key: 'sql', title: 'SQL' },
        { key: 'javascript', title: 'JavaScript' },
        { key: 'text/plain', title: '纯文本' },
      ],
    };
  },
  computed: {
    ...mapDateTable('DTSC', []),
    tabs() {
      return [
        { key: 'list', title: '列表字段' },
        { key: 'form', title: '表单字段' },
        { key: 'query', title: '查询字段' },
      ];
    },
    resItemForSel() {
      return { ID: this.resourceId, RESOURCENAME: this.resourceName };
    },
    // 占宽下拉：数据字典「字段占宽」(D0708)。注意字典 ITEMVALUE 是字符串 '1'/'2'
    colspanOptions() {
      const d = (this.$store.state.app && this.$store.state.app.dicts['字段占宽']) || {};
      return Object.keys(d).map(k => ({ key: +k, title: d[k] }));
    },
    // 系统字典名列表，供 select 类型字段的数据源选择
    dictNameOptions() {
      const dicts = (this.$store.state.app && this.$store.state.app.dicts) || {};
      return Object.keys(dicts).map(k => ({ key: k, title: k }));
    },
    // 当前选中字典的选项列表（用于数据范围多选）
    currentDictItemOptions() {
      const dictName = this.getSelectDictName(this.currentRow && this.currentRow.SELECTDATA);
      if (!dictName) return [];
      const dicts = (this.$store.state.app && this.$store.state.app.dicts) || {};
      const dict = dicts[dictName];
      if (!dict) return [];
      return Object.keys(dict).map(k => ({ key: k, title: dict[k] }));
    },
    // 代码编辑类型的语言设置（读写 SELECTDATA JSON）
    codeLanguage: {
      get() {
        try {
          const parsed = JSON.parse(this.currentRow.SELECTDATA);
          return (parsed && parsed.language) || 'sql';
        } catch (e) { return 'sql' }
      },
      set(v) {},
    },
    // tableblock 按钮显隐（读写 SELECTDATA.showButtons）
    tableBlockBtnAdd: {
      get() { return this._tbBtn('add') },
      set(v) { this._setTbBtn('add', v) },
    },
    tableBlockBtnRemove: {
      get() { return this._tbBtn('remove') },
      set(v) { this._setTbBtn('remove', v) },
    },
    tableBlockBtnUp: {
      get() { return this._tbBtn('up') },
      set(v) { this._setTbBtn('up', v) },
    },
    tableBlockBtnDown: {
      get() { return this._tbBtn('down') },
      set(v) { this._setTbBtn('down', v) },
    },
    // tableblock 自定义按钮摘要
    tbButtonsSummary() {
      const btns = this.selConfig().buttons || [];
      if (!btns.length) return '';
      return btns.map(b => b.label || b.code).join('、');
    },
    // 默认参数：把 SELECTDATA.defaultParams 对象序列化为文本展示
    defaultParamsText() {
      const dp = this.selConfig().defaultParams;
      if (!dp || typeof dp !== 'object') return '';
      return JSON.stringify(dp);
    },
    // 列表 Tab 实际写入/读取的排序字段：
    // 资源历史上有两套配置：老资源 LISTSORT 为空、用 ENTRYNUM 作列表排序；
    // 新资源直接配 LISTSORT。哪个被用过就用哪个，避免改变资源的配置约定。
    listSortField() {
      const hasListsort = this.DTSC.some(r => +r.LISTSORT > 0);
      return hasListsort ? 'LISTSORT' : 'ENTRYNUM';
    },
    currentSortKey() {
      if (this.activeTab === 'list') return this.listSortField;
      return this.activeTab === 'form' ? 'EDITSORT' : 'QUERYSORT';
    },
    sortValueOf(row) {
      return +row[this.currentSortKey] || 0;
    },
    tabTitle() {
      const t = this.tabs.find(x => x.key === this.activeTab);
      return t ? t.title : '';
    },
    previewTip() {
      return '修改左侧配置后此处自动刷新';
    },
    visibleFields() {
      const sortKey = this.currentSortKey;
      if (this.activeTab === 'list') {
        // 列表 Tab：二级排序
        // 已勾选：优先 LISTSORT，LISTSORT 相同或为 0 时按 ENTRYNUM
        // 未勾选：按 ENTRYNUM 保留原始顺序
        const checked = this.DTSC.filter(r => this.listDisplayOf(r));
        checked.sort((a, b) => {
          const la = +a.LISTSORT || 0;
          const lb = +b.LISTSORT || 0;
          if (la && lb) return la - lb; // 都有 LISTSORT，按它排
          if (la) return -1; // a 有 b 没有，a 在前
          if (lb) return 1; // b 有 a 没有，b 在前
          return (+a.ENTRYNUM || 0) - (+b.ENTRYNUM || 0); // 都没有，按 ENTRYNUM
        });
        const unchecked = this.DTSC.filter(r => !this.listDisplayOf(r));
        unchecked.sort((a, b) => (+a.ENTRYNUM || 0) - (+b.ENTRYNUM || 0));
        return checked.concat(unchecked);
      }
      // 表单/查询 Tab：已勾选按排序字段排，未勾选按 ENTRYNUM 保留原始顺序
      const checked = this.DTSC.filter(r => +r[sortKey] > 0);
      checked.sort((a, b) => +a[sortKey] - +b[sortKey]);
      const unchecked = this.DTSC.filter(r => !r[sortKey] || +r[sortKey] === 0);
      unchecked.sort((a, b) => (+a.ENTRYNUM || 0) - (+b.ENTRYNUM || 0));
      return checked.concat(unchecked);
    },
    previewColumns() {
      // 列表预览：按 DISPLAYINLIST 过滤，再按 LISTSORT 优先 + ENTRYNUM 二级排序
      let fields;
      if (this.activeTab === 'list') {
        fields = this.DTSC.filter(r => this.listDisplayOf(r));
        fields.sort((a, b) => {
          const la = +a.LISTSORT || 0;
          const lb = +b.LISTSORT || 0;
          if (la && lb) return la - lb;
          if (la) return -1;
          if (lb) return 1;
          return (+a.ENTRYNUM || 0) - (+b.ENTRYNUM || 0);
        });
      } else {
        const sortKey = this.currentSortKey;
        fields = this.DTSC.filter(r => +r[sortKey] > 0);
        fields.sort((a, b) => +a[sortKey] - +b[sortKey]);
      }
      return Gen.getTableColumns(fields, { editInfo: { editIndex: -1, edit: false } });
    },
    previewFormFields() {
      const fields = this.DTSC.filter(r => +r.EDITSORT > 0);
      fields.sort((a, b) => +a.EDITSORT - +b.EDITSORT);
      return Gen.getFormFields(fields);
    },
    // 当前分组字段转成 rs-form-edit 可用的格式（只渲染当前分组）
    currentFormGroupPreviewFields() {
      if (!this.currentFormGroup) return [];
      return Gen.getFormFields(this.currentFormGroup.items);
    },
    // 表单画布：按 EDITGROUP 分组，空分组名归入"基本信息"；自定义空分组放最后
    formGroups() {
      const checked = this.DTSC.filter(r => +r.EDITSORT > 0);
      checked.sort((a, b) => +a.EDITSORT - +b.EDITSORT);
      const map = {};
      const groups = [];
      // 有字段的分组（按字段出现顺序）
      checked.forEach(r => {
        const g = (r.EDITGROUP || '').trim() || '基本信息';
        if (!map[g]) {
          map[g] = { name: g, items: [] };
          groups.push(map[g]);
        }
        map[g].items.push(r);
      });
      // 用户自定义的空分组（还没字段的，放最后）
      this.customGroups.forEach(name => {
        if (!map[name]) {
          map[name] = { name, items: [] };
          groups.push(map[name]);
        }
      });
      return groups;
    },
    // 当前激活分组对象
    currentFormGroup() {
      const name = this.activeFormGroup;
      return this.formGroups.find(g => g.name === name) || this.formGroups[0] || null;
    },
    // 表单分组 Tab 数据 {name: title}
    formTabDatas() {
      const tabs = {};
      this.formGroups.forEach(g => { tabs[g.name] = g.name; });
      return tabs;
    },
    // 本地字段候选：当前资源所有字段名（自动去重、去空）
    localFieldOptions() {
      const arr = this.DTSC.map(r => r.RESFIELDNAME || r.FIELDNAME).filter(Boolean);
      const set = new Set();
      const ret = [];
      arr.forEach(k => {
        if (!set.has(k)) {
          set.add(k);
          ret.push({ key: k, title: k });
        }
      });
      return ret;
    },
    // 远程字段候选：根据 EDITTYPE 区分
    remoteFieldOptions() {
      const editType = this.currentRow && this.currentRow.EDITTYPE;
      if (editType === 'fileupload' || editType === 'imageupload' || editType === 'fileuploadtpl') {
        return [
          { key: 'id', title: 'id(文件ID)' },
          { key: 'name', title: 'name(文件名)' },
        ];
      }
      return this.fieldOptions;
    },
    // 动作代码弹窗：功能模块选项（FUNCTYPE=2 的菜单项）
    actionFuncOptions() {
      const menus = (this.$store.state.app && this.$store.state.app.omenus) || [];
      return menus.filter(m => m.FUNCTYPE === 2);
    },
    // 动作代码弹窗：功能点按 FUNCID 分组
    actionFuncPointMap() {
      const points = (this.$store.state.app && this.$store.state.app.ofpoints) || [];
      const map = {};
      points.forEach(p => {
        if (!map[p.FUNCID]) map[p.FUNCID] = [];
        map[p.FUNCID].push(p);
      });
      return map;
    },
    // 权限码 PERCODE = FUNCCODE/FUNCPOINTCODE，拆出功能模块编码
    perFuncCode() {
      const code = (this.currentRow && this.currentRow.PERCODE) || '';
      return code.split('/')[0] || '';
    },
    perPointCode() {
      const code = (this.currentRow && this.currentRow.PERCODE) || '';
      return code.split('/')[1] || '';
    },
    // 功能点选项：按所选功能模块的 ID 过滤 ofpoints
    perPointOptions() {
      const fc = this.perFuncCode;
      if (!fc) return [];
      const func = this.actionFuncOptions.find(f => f.FUNCCODE === fc);
      if (!func) return [];
      const points = (this.$store.state.app && this.$store.state.app.ofpoints) || [];
      return points.filter(p => p.FUNCID === func.ID);
    },
    previewFormPath() {
      const self = this;
      return {
        data: [self.mockFormModel],
        add() {},
        setValue(k, v) { self.$set(self.mockFormModel, k, v) },
      };
    },
    mockRows() {
      const cols = this.previewColumns;
      return [1, 2, 3].map((n, idx) => {
        const row = { _idx: idx + 1 };
        cols.forEach(c => {
          const t = c.type;
          const k = c.prop;
          if (t === 'select') {
            const datas = this.parseSelectDatasFromText(c.selectData);
            row[k] = datas.length > 0 ? datas[idx % datas.length].key : `选项${n}`;
          } else if (t === 'checkbox') {
            row[k] = idx % 2;
          } else if (t === 'number') {
            row[k] = n * 100;
          } else if (t === 'datepicker') {
            row[k] = `2026-06-1${n}`;
          } else if (t === 'image') {
            row[k] = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="60" height="20"><rect width="60" height="20" fill="%23eee"/><text x="30" y="14" font-size="10" fill="%23999" text-anchor="middle">IMG</text></svg>';
          } else if (t === 'action' || t === 'index') {
            row[k] = '';
          } else {
            row[k] = `示例${n}`;
          }
        });
        return row;
      });
    },
  },
  watch: {
    activeTab() {
      if (this.currentRow && this.visibleFields.indexOf(this.currentRow) < 0) {
        this.currentRow = null;
      }
      // 切换到 form Tab 时初始化当前分组
      if (this.activeTab === 'form') {
        const g = this.formGroups[0];
        this.activeFormGroup = g ? g.name : '';
      }
    },
    // 选中 autocomplete/multiautocomplete/treepicker 字段时，自动加载模块列表和该模块的接口列表
    async currentRow(row) {
      if (!row) return;
      const t = row.EDITTYPE;
      if (t === 'autocomplete' || t === 'multiautocomplete' || t === 'treepicker') {
        this.onSelPanelShow();
      }
      // tableblock：加载目标模块的子表数据源列表
      if (t === 'tableblock') {
        this.loadModules();
        const cfg = this.selConfig();
        if (cfg.targetModule) this.loadSubtables(cfg.targetModule);
      }
      // fileupload/imageupload 子表模式：预加载目标模块的子表列表 + 子表字段
      if (t === 'fileupload' || t === 'imageupload') {
        const tm = this.uploaderProp('targetModule');
        const sub = this.uploaderProp('subtable');
        if (this.uploaderProp('mode') === 'subtable') {
          this.loadModules();
          if (tm) {
            await this.loadSubtables(tm);
            if (sub) await this.loadSubtableFields(sub, tm);
          }
        }
      }
    },
    // 同步 saving 状态到父组件（让 footer 的保存按钮显示 loading）
    saving(v) {
      this.$emit('saving-change', v);
    },
    previewFormFields: {
      immediate: true,
      handler(fields) {
        const old = this.mockFormModel || {};
        const m = {};
        fields.forEach(f => {
          const k = f.props.key;
          m[k] = old[k] !== undefined ? old[k] : '';
        });
        this.mockFormModel = m;
      },
    },
    visibleFields: {
      immediate: true,
      handler(arr) {
        const old = this.mockQueryModel || {};
        const m = {};
        arr.forEach(r => {
          if (+r.QUERYSORT > 0) {
            const k = r.RESFIELDNAME || r.FIELDNAME;
            if (k) m[k] = old[k] !== undefined ? old[k] : '';
          }
        });
        this.mockQueryModel = m;
      },
    },
    formGroups: {
      immediate: true,
      handler(groups) {
        if (!groups || !groups.length) {
          this.activeFormGroup = '';
          return;
        }
        if (!groups.find(g => g.name === this.activeFormGroup)) {
          this.activeFormGroup = groups[0].name;
        }
      },
    },
  },
  async mounted() {
    await this.$callAction({ action: `${Constants.STORE_NAME}/queryDTSC`,
      param: {
        RESOURCEID: this.resourceId,
      },
      isBusy: false });
    // 初始化左侧面板宽度
    this.$nextTick(() => {
      const el = this.$el.querySelector('.uis-left');
      if (el) this.leftWidth = el.offsetWidth;
    });
    // 预加载业务模块列表，供 autocomplete/treepicker 字段配置使用
    this.loadModules();
  },
  methods: {
    rowKey(row, fallback) {
      return row.ID || row.RESFIELDID || ('r' + fallback);
    },
    // 解析 SELECTDATA 为选择器配置对象
    selConfig() {
      if (!this.currentRow || !this.currentRow.SELECTDATA) return {};
      const raw = this.currentRow.SELECTDATA;
      try {
        const parsed = JSON.parse(raw);
        if (parsed && typeof parsed === 'object') return parsed;
      } catch (e) {}
      // 字符串格式（预设名）
      const preset = SEL_TYPES.concat(TREE_SEL_TYPES).find(t => t.key === raw);
      if (preset) {
        return {
          module: preset.module,
          apiCode: preset.apiCode,
          keyName: preset.keyName,
          titleName: preset.titleName,
          parentName: preset.parentName,
        };
      }
      return {};
    },
    // 读/改 SELECTDATA 中的某个键
    getSelField(k) {
      const cfg = this.selConfig();
      return cfg[k] != null ? cfg[k] : '';
    },
    setSelField(k, v) {
      const cfg = this.selConfig();
      cfg[k] = v;
      this.setField('SELECTDATA', JSON.stringify(cfg));
    },
    // 选中预设时一次性写入 module/apiCode/keyName/titleName
    async applySelPreset(presetKey) {
      const preset = SEL_TYPES.concat(TREE_SEL_TYPES).find(t => t.key === presetKey);
      if (!preset) return;
      const cfg = {
        module: preset.module,
        apiCode: preset.apiCode,
        keyName: preset.keyName,
        titleName: preset.titleName,
      };
      if (preset.parentName) cfg.parentName = preset.parentName;
      this.setField('SELECTDATA', JSON.stringify(cfg));
      // 先加载该模块的接口列表（loadFieldOptions 依赖 apiListOfModule 找 PATHNAME）
      await this.loadApis(preset.module);
      this.loadFieldOptions(preset.module, preset.apiCode);
    },
    async loadModules() {
      if (this.moduleList.length > 0) return;
      try {
        const list = await queryModules();
        this.moduleList = list;
        console.log('[uiSetFull] loadModules:', list.length, '条');
      } catch (e) {
        this.moduleList = [];
        console.error('[uiSetFull] loadModules 失败:', e);
      }
    },
    async loadApis(moduleCode) {
      if (!moduleCode) {
        this.apiListOfModule = [];
        return;
      }
      try {
        const list = await queryApis(moduleCode);
        this.apiListOfModule = list;
        console.log('[uiSetFull] loadApis:', moduleCode, list.length, '条');
      } catch (e) {
        this.apiListOfModule = [];
        console.error('[uiSetFull] loadApis 失败:', e);
      }
    },
    // 当 autocomplete/treepicker 面板出现时触发加载
    async onSelPanelShow() {
      this.loadModules();
      const cfg = this.selConfig();
      // 来源模块：驱动接口 + 字段（下拉搜索数据源）
      if (cfg.module) this.loadApis(cfg.module);
      if (cfg.module && cfg.apiCode) this.loadFieldOptions(cfg.module, cfg.apiCode);
      // 目标模块：驱动子表数据源（可能与来源模块不同）
      if (cfg.targetModule) await this.loadSubtables(cfg.targetModule);
      if (cfg.subtable) await this.loadSubtableFields(cfg.subtable, cfg.targetModule);
    },
    // 来源模块切换：只刷新接口列表（子表属目标模块，由目标模块选择器负责）
    onSelModuleChange(moduleCode) {
      this.setSelField('module', moduleCode);
      this.setSelField('apiCode', '');
      this.fieldOptions = [];
      this.loadApis(moduleCode);
    },
    // 目标模块切换：刷新该模块的子表数据源列表，清空已选子表/映射
    onTargetModuleChange(moduleCode) {
      this.setSelField('targetModule', moduleCode);
      this.setSelField('subtable', '');
      this.setSelField('subMappings', '');
      this.subtableFieldOptions = [];
      this.loadSubtables(moduleCode);
    },
    // 加载模块的子表数据源（MODPATH 中 PATHNAME 以 DTS 开头的项）
    async loadSubtables(moduleCode) {
      if (!moduleCode) { this.subtableOptions = []; return }
      try {
        if (!this.$store.state.app.modules[moduleCode]) {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch('app/initModule', moduleCode);
        }
        const modData = this.$store.state.app.modules[moduleCode];
        const paths = (modData && modData.MODPATH) || [];
        this.subtableOptions = paths
          .filter(p => /^DTS/.test(p.PATHNAME))
          .map(p => ({ key: p.PATHNAME, title: `${p.PATHNAME} (${p.RESOURCENAME || ''})`, resourceName: p.RESOURCENAME }));
      } catch (e) {
        this.subtableOptions = [];
      }
    },
    // multiautocomplete 子表切换
    async onSubtableChange(pathName) {
      this.setSelField('subtable', pathName);
      this.setSelField('subMappings', '');
      await this.loadSubtableFields(pathName, this.getSelField('targetModule'));
    },
    // 根据子表 PATHNAME 找到资源名，加载其 scm 字段
    async loadSubtableFields(pathName, moduleCode) {
      // 优先从 subtableOptions 找 resourceName（已加载的情况）
      let resName = '';
      const opt = this.subtableOptions.find(o => o.key === pathName);
      if (opt) resName = opt.resourceName;
      // 降级：从模块 MODPATH 直接查
      if (!resName && moduleCode) {
        try {
          if (!this.$store.state.app.modules[moduleCode]) {
            // eslint-disable-next-line no-restricted-syntax
            await this.$store.dispatch('app/initModule', moduleCode);
          }
          const modData = this.$store.state.app.modules[moduleCode];
          const paths = (modData && modData.MODPATH) || [];
          const found = paths.find(p => p.PATHNAME === pathName);
          if (found) resName = found.RESOURCENAME;
        } catch (e) {}
      }
      if (!resName) { this.subtableFieldOptions = []; this.uploadSubtableFieldOptions = []; return }
      try {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', [resName]);
        const fields = this.$store.state.app.scms[resName] || [];
        const opts = fields.map(f => ({ key: f.FIELDNAME, title: f.FIELDNAME }));
        this.subtableFieldOptions = opts;
        this.uploadSubtableFieldOptions = opts;
      } catch (e) {
        this.subtableFieldOptions = [];
        this.uploadSubtableFieldOptions = [];
      }
    },
    // 加载接口返回字段列表（值字段/显示字段下拉用）
    // 加载接口对应数据源的字段列表（值字段/显示字段/传入字段下拉用）
    // 链路：apiCode → apiListOfModule 找 PATHNAME → 模块 MODPATH 找 RESOURCENAME → 查 tss_resfield 字段
    async loadFieldOptions(module, apiCode) {
      if (!module || !apiCode) {
        this.fieldOptions = [];
        return;
      }
      try {
        // 1. 接口列表里找该 apiCode 的 PATHNAME；若列表空则先加载该模块接口
        let api = this.apiListOfModule.find(a => a.APICODE === apiCode);
        if (!api) {
          await this.loadApis(module);
          api = this.apiListOfModule.find(a => a.APICODE === apiCode);
        }
        const pathName = api && api.PATHNAME;
        if (!pathName) { this.fieldOptions = []; return }
        // 2. 确保模块已加载，取 MODPATH
        if (!this.$store.state.app.modules[module]) {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch('app/initModule', module);
        }
        const modData = this.$store.state.app.modules[module];
        const paths = (modData && modData.MODPATH) || [];
        // 3. PATHNAME → RESOURCENAME（优先精确匹配，回退 QRY）
        let resName = '';
        const exact = paths.find(p => p.PATHNAME === pathName);
        if (exact) resName = exact.RESOURCENAME;
        if (!resName) {
          const qry = paths.find(p => p.PATHNAME === 'QRY' || p.PATHNAME === 'QQRY');
          if (qry) resName = qry.RESOURCENAME;
        }
        if (!resName) { this.fieldOptions = []; return }
        // 4. 按 RESOURCENAME 查字段（F00 同时匹配 ID 和 RESOURCENAME）
        const fields = await queryFieldsByResourceId(resName);
        this.fieldOptions = fields;
      } catch (e) {
        this.fieldOptions = [];
      }
    },
    // apiCode 变化时：写入并加载远程字段列表
    onSelApiChange(apiCode) {
      this.setSelField('apiCode', apiCode);
      const module = this.getSelField('module');
      this.loadFieldOptions(module, apiCode);
    },
    // === 下拉数据：字典/自定义数据 辅助方法 ===
    // 判断 SELECTDATA 是否是字典（纯字典名 或 {dict:...,items:[...]} 格式）
    isSelectDict(val) {
      if (!val) return false;
      if (this.isDictName(val)) return true;
      try {
        const parsed = JSON.parse(val);
        return parsed && typeof parsed === 'object' && !!parsed.dict;
      } catch (e) { return false; }
    },
    // 兼容旧调用
    isDictName(val) {
      if (!val) return false;
      const dicts = (this.$store.state.app && this.$store.state.app.dicts) || {};
      return !!dicts[val];
    },
    // 从 SELECTDATA 提取字典名（纯字符串 或 JSON{dict:...}）
    getSelectDictName(val) {
      if (!val) return '';
      if (this.isDictName(val)) return val;
      try {
        const parsed = JSON.parse(val);
        if (parsed && parsed.dict && this.isDictName(parsed.dict)) return parsed.dict;
      } catch (e) {}
      return '';
    },
    // 从 SELECTDATA 提取数据范围 items（JSON{dict:...,items:[...]} 格式）
    getSelectDictItems(val) {
      if (!val) return [];
      try {
        const parsed = JSON.parse(val);
        if (parsed && parsed.items && Array.isArray(parsed.items)) return parsed.items;
      } catch (e) {}
      return [];
    },
    // 选择字典名后写入 SELECTDATA
    onDictSelect(dictName) {
      if (dictName) {
        this.setField('SELECTDATA', dictName);
      } else {
        this.setField('SELECTDATA', '');
      }
    },
    // 数据范围多选变化：更新 SELECTDATA
    onDictRangeChange(items) {
      const dictName = this.getSelectDictName(this.currentRow && this.currentRow.SELECTDATA);
      if (!dictName) return;
      var arr = items || [];
      if (arr.length === 0) {
        // 无选中=全量字典，存纯字典名
        this.setField('SELECTDATA', dictName);
      } else {
        // 有选中=过滤字典，存 JSON
        this.setField('SELECTDATA', JSON.stringify({ dict: dictName, items: arr }));
      }
    },
    // 打开自定义下拉数据弹窗
    openCustomDataDialog() {
      if (!this.currentRow) return;
      var rows = [];
      var raw = this.currentRow.SELECTDATA || '';
      // 解析现有 SELECTDATA 为行
      if (raw && !this.isSelectDict(raw)) {
        try {
          var parsed = JSON.parse(raw);
          if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
            Object.keys(parsed).forEach(function(k) {
              rows.push({ key: k, title: String(parsed[k]) });
            });
          }
        } catch (e) {
          // k:v,k:v 格式
          if (raw.indexOf(':') > 0) {
            raw.split(',').forEach(function(seg) {
              var parts = seg.split(':');
              if (parts.length >= 2) {
                rows.push({ key: (parts[0] || '').trim(), title: (parts[1] || '').trim() });
              }
            });
          }
        }
      }
      if (rows.length === 0) rows.push({ key: '', title: '' });
      this.customDataRows = rows;
      this.customDataShow = true;
    },
    // 保存自定义下拉数据
    saveCustomDataDialog() {
      var rows = (this.customDataRows || []).filter(function(r) { return r.key; });
      if (rows.length === 0) {
        this.setField('SELECTDATA', '');
      } else {
        var obj = {};
        rows.forEach(function(r) { obj[r.key] = r.title || r.key; });
        this.setField('SELECTDATA', JSON.stringify(obj));
      }
      this.customDataShow = false;
    },
    // 打开"更新字段"配置弹窗
    openUpdateFieldsDialog() {
      if (!this.currentRow) return;
      const raw = this.currentRow.UPDATEFIELDS || '';
      this.updateFieldsRows = raw.split(';')
        .filter(seg => seg && seg.indexOf(',') >= 0)
        .map(seg => {
          const [local, remote] = seg.split(',');
          return {
            local: (local || '').trim(),
            remote: (remote || '').trim(),
          };
        });
      this.updateFieldsShow = true;
    },
    saveUpdateFieldsDialog() {
      const s = this.updateFieldsRows
        .filter(r => r.local && r.remote)
        .map(r => `${r.local},${r.remote}`)
        .join(';');
      this.setField('UPDATEFIELDS', s);
      this.updateFieldsShow = false;
    },
    // 打开"传入字段"配置弹窗：把 "local,remote;..." 拆成行
    openParamMappingsDialog() {
      const raw = this.getSelField('paramMappings') || '';
      this.paramMappingsRows = raw.split(';').map(s => s.trim()).filter(Boolean).map(seg => {
        const [local, remote] = seg.split(',').map(x => (x || '').trim());
        return { local, remote };
      });
      if (!this.paramMappingsRows.length) this.paramMappingsRows = [{ local: '', remote: '' }];
      this.paramMappingsShow = true;
    },
    saveParamMappingsDialog() {
      const s = this.paramMappingsRows
        .filter(r => r.local && r.remote)
        .map(r => `${r.local},${r.remote}`)
        .join(';');
      this.setSelField('paramMappings', s);
      this.paramMappingsShow = false;
    },
    // 默认参数：文本→对象存入 SELECTDATA.defaultParams（解析失败则存 null 清空）
    onDefaultParamsInput(text) {
      const trimmed = (text || '').trim();
      if (!trimmed) {
        this.setSelField('defaultParams', null);
        return;
      }
      try {
        const parsed = JSON.parse(trimmed);
        if (parsed && typeof parsed === 'object') {
          this.setSelField('defaultParams', parsed);
        }
      } catch (e) {
        // JSON 不完整时不写入，等用户输完
      }
    },
    // 打开"子表映射字段"配置弹窗：把 "sub,remote;sub,remote" 拆成行（同 UPDATEFIELDS 格式）
    openSubMappingsDialog() {
      const raw = this.getSelField('subMappings') || '';
      this.subMappingsRows = raw.split(';').map(s => s.trim()).filter(Boolean).map(seg => {
        const [sub, remote] = seg.split(',').map(x => (x || '').trim());
        return { sub, remote };
      });
      if (!this.subMappingsRows.length) this.subMappingsRows = [{ sub: '', remote: '' }];
      this.subMappingsShow = true;
    },
    saveSubMappingsDialog() {
      const s = this.subMappingsRows
        .filter(r => r.sub && r.remote)
        .map(r => `${r.sub},${r.remote}`)
        .join(';');
      this.setSelField('subMappings', s);
      this.subMappingsShow = false;
    },
    // 上传多文件/子表互斥
    onUploaderMultiFileChange(val) {
      this.setUploaderProp('multifile', val);
      if (val) this.setUploaderProp('mode', ''); // 关闭子表模式
    },
    onUploaderSubtableChange(val) {
      if (val) {
        this.setUploaderProp('mode', 'subtable');
        this.setUploaderProp('multifile', false); // 关闭多文件模式
        this.loadModules(); // 加载模块列表供目标模块下拉
        // 如果已有 targetModule，加载其子表
        const tm = this.uploaderProp('targetModule');
        if (tm) this.loadSubtables(tm);
      } else {
        this.setUploaderProp('mode', '');
        this.setUploaderProp('subtable', '');
        this.setUploaderProp('subMappings', '');
        this.setUploaderProp('targetModule', '');
      }
    },
    // 上传子表：目标模块切换
    onUploadTargetModuleChange(moduleCode) {
      this.setUploaderProp('targetModule', moduleCode);
      this.setUploaderProp('subtable', '');
      this.setUploaderProp('subMappings', '');
      this.subtableFieldOptions = [];
      this.loadSubtables(moduleCode);
    },
    // 上传子表：子表切换 → 加载子表字段
    async onUploadSubtableChange(pathName) {
      this.setUploaderProp('subtable', pathName);
      this.setUploaderProp('subMappings', '');
      await this.loadSubtableFields(pathName, this.uploaderProp('targetModule'));
    },
    openUploadMappingsDialog() {
      const raw = this.uploaderProp('subMappings') || '';
      this.uploadMappingsRows = raw.split(';').map(s => s.trim()).filter(Boolean).map(seg => {
        const [sub, remote] = seg.split(',').map(x => (x || '').trim());
        return { sub, remote };
      });
      if (!this.uploadMappingsRows.length) this.uploadMappingsRows = [{ sub: '', remote: '' }];
      // 确保上传子表字段下拉已加载
      const sub = this.uploaderProp('subtable');
      if (sub && !this.uploadSubtableFieldOptions.length) {
        this.loadSubtableFields(sub, this.uploaderProp('targetModule'));
      }
      this.uploadMappingsShow = true;
    },
    saveUploadMappingsDialog() {
      const s = this.uploadMappingsRows
        .filter(r => r.sub && r.remote)
        .map(r => `${r.sub},${r.remote}`)
        .join(';');
      this.setUploaderProp('subMappings', s);
      this.uploadMappingsShow = false;
    },
    // 读取上传配置项（SELECTDATA 是 JSON）
    uploaderProp(propKey) {
      if (!this.currentRow || !this.currentRow.SELECTDATA) return '';
      try {
        const parsed = JSON.parse(this.currentRow.SELECTDATA);
        return parsed && parsed[propKey] != null ? parsed[propKey] : '';
      } catch (e) {
        return '';
      }
    },
    // 修改上传配置项（保持其它键不变）
    setUploaderProp(propKey, val) {
      if (!this.currentRow) return;
      let cfg = {};
      try {
        const parsed = JSON.parse(this.currentRow.SELECTDATA);
        if (parsed && typeof parsed === 'object') cfg = parsed;
      } catch (e) {}
      cfg[propKey] = val;
      this.setField('SELECTDATA', JSON.stringify(cfg));
    },
    // fileuploadtpl 配置读写（同 uploaderProp/setUploaderProp，SELECTDATA JSON）
    uploaderTplProp(propKey) {
      return this.uploaderProp(propKey);
    },
    setUploaderTplProp(propKey, val) {
      this.setUploaderProp(propKey, val);
    },
    // 代码语言切换
    onCodeLangChange(val) {
      const config = JSON.stringify({ language: val });
      this.setField('SELECTDATA', config);
    },
    // tableblock：目标模块切换，加载该模块子表数据源
    async onTableBlockModuleChange(moduleCode) {
      this.setSelField('targetModule', moduleCode);
      this.setSelField('subtable', '');
      await this.loadSubtables(moduleCode);
    },
    // 权限码：功能模块切换，清空功能点，写 FUNCCODE/
    onPerFuncChange(funcCode) {
      const point = this.perPointCode;
      const str = point ? `${funcCode}/${point}` : `${funcCode}/`;
      this.$set(this.currentRow, 'PERCODE', str);
      this.$DTSC.setValue('PERCODE', str, this.currentRow);
    },
    // 权限码：功能点切换，写 FUNCCODE/FUNCPOINTCODE
    onPerPointChange(pointCode) {
      const str = `${this.perFuncCode}/${pointCode}`;
      this.$set(this.currentRow, 'PERCODE', str);
      this.$DTSC.setValue('PERCODE', str, this.currentRow);
    },
    // tableblock：读 showButtons[k]，默认 true
    _tbBtn(key) {
      const sb = this.selConfig().showButtons || {};
      return sb[key] !== false;
    },
    _setTbBtn(key, v) {
      const cfg = this.selConfig();
      cfg.showButtons = Object.assign({ add: true, remove: true, up: true, down: true }, cfg.showButtons || {});
      cfg.showButtons[key] = v;
      const str = JSON.stringify(cfg);
      // 先 $set 保证 currentRow.SELECTDATA 响应式（新属性情况），再 setValue 记录变更
      this.$set(this.currentRow, 'SELECTDATA', str);
      this.$DTSC.setValue('SELECTDATA', str, this.currentRow);
    },
    // tableblock 自定义按钮弹窗：每行 {label, code, actionCode, per}
    openTbButtonsDialog() {
      const btns = this.selConfig().buttons || [];
      this.tbButtonsRows = btns.map(b => ({
        label: b.label || '',
        code: b.code || '',
        actionCode: b.actionCode || '',
        per: b.per || '',
      }));
      if (!this.tbButtonsRows.length) this.tbButtonsRows = [{ label: '', code: '', actionCode: '', per: '' }];
      this.tbButtonsShow = true;
    },
    saveTbButtonsDialog() {
      const cfg = this.selConfig();
      cfg.buttons = this.tbButtonsRows
        .filter(r => r.label || r.code || r.actionCode)
        .map(r => ({ label: r.label, code: r.code, actionCode: r.actionCode, per: r.per }));
      const str = JSON.stringify(cfg);
      this.$set(this.currentRow, 'SELECTDATA', str);
      this.$DTSC.setValue('SELECTDATA', str, this.currentRow);
      this.tbButtonsShow = false;
    },
    isChecked(row) {
      if (this.activeTab === 'list') return this.listDisplayOf(row);
      const v = row[this.currentSortKey];
      return v != null && +v > 0;
    },
    // 列表 Tab 勾选规则：DISPLAYINLIST 有值（0/1）按它判断；null/undefined 回退 SHOWLENGTH
    listDisplayOf(row) {
      if (row.DISPLAYINLIST !== null && row.DISPLAYINLIST !== undefined) {
        return +row.DISPLAYINLIST === 1;
      }
      const sl = row.SHOWLENGTH;
      return sl != null && (sl + '') !== '0' && (sl + '') !== '';
    },
    setField(field, val) {
      if (!this.currentRow) return;
      this._setRowField(this.currentRow, field, val);
    },
    // 统一写字段：DataTable.setValue + 强制触发 Vue 响应式更新
    _setRowField(row, field, val) {
      this.$DTSC.setValue(field, val, row);
      // Vue 2 对数组内对象的新属性变化不会触发 computed 重算，
      // 用 $set 替换数组同一位置的对象引用，强制 DTSC computed 刷新
      const idx = this.DTSC.indexOf(row);
      if (idx >= 0) {
        this.$set(this.DTSC, idx, row);
      }
    },
    onToggleVisible(row) {
      // 列表 Tab：用 DISPLAYINLIST 管显示，同时同步 LISTSORT/ENTRYNUM 管排序
      if (this.activeTab === 'list') {
        const sortKey = this.listSortField;
        const willShow = !this.listDisplayOf(row);
        this._setRowField(row, 'DISPLAYINLIST', willShow ? 1 : 0);
        if (willShow) {
          let max = 0;
          this.DTSC.forEach(r => {
            const v = +r[sortKey] || 0;
            if (v > max) max = v;
          });
          this._setRowField(row, sortKey, max + 1);
        } else {
          this._setRowField(row, sortKey, 0);
          this.compactSort();
        }
        return;
      }
      const sortKey = this.currentSortKey;
      const cur = +row[sortKey] || 0;
      if (cur > 0) {
        this._setRowField(row, sortKey, 0);
        this.compactSort();
      } else {
        let max = 0;
        this.DTSC.forEach(r => {
          const v = +r[sortKey] || 0;
          if (v > max) max = v;
        });
        this._setRowField(row, sortKey, max + 1);
      }
    },
    compactSort() {
      const sortKey = this.currentSortKey;
      const arr = this.DTSC.filter(r => +r[sortKey] > 0);
      arr.sort((a, b) => +a[sortKey] - +b[sortKey]);
      arr.forEach((r, i) => this._setRowField(r, sortKey, i + 1));
    },
    moveUp(row) {
      const arr = this.visibleFields.filter(r => this.isChecked(r));
      const idx = arr.indexOf(row);
      if (idx <= 0) return;
      this.swapSort(arr[idx - 1], row);
    },
    moveDown(row) {
      const arr = this.visibleFields.filter(r => this.isChecked(r));
      const idx = arr.indexOf(row);
      if (idx < 0 || idx >= arr.length - 1) return;
      this.swapSort(row, arr[idx + 1]);
    },
    swapSort(a, b) {
      const sortKey = this.currentSortKey;
      const sa = +a[sortKey];
      const sb = +b[sortKey];
      this._setRowField(a, sortKey, sb);
      this._setRowField(b, sortKey, sa);
    },
    onDragStart(idx, e) {
      // 仅对已勾选行允许拖拽排序
      const row = this.visibleFields[idx];
      if (!this.isChecked(row)) {
        e.preventDefault();
        return;
      }
      this.dragIndex = idx;
      this.dragOverPos = '';
      try { e.dataTransfer.effectAllowed = 'move' } catch (err) {}
    },
    onDragOver(idx, e) {
      if (this.dragIndex < 0) return;
      const row = this.visibleFields[idx];
      if (!this.isChecked(row)) return;
      e.preventDefault();
      this.dragOverIndex = idx;
      const rect = e.currentTarget.getBoundingClientRect();
      const midY = rect.top + rect.height / 2;
      this.dragOverPos = e.clientY < midY ? 'top' : 'bottom';
    },
    onDragLeave(idx) {
      if (this.dragOverIndex === idx) {
        this.dragOverPos = '';
      }
    },
    onDrop(idx) {
      if (this.dragIndex < 0 || this.dragIndex === idx) {
        this.dragIndex = -1;
        this.dragOverPos = '';
        return;
      }
      const arr = this.visibleFields;
      const src = arr[this.dragIndex];
      if (!this.isChecked(src)) {
        this.dragIndex = -1;
        this.dragOverPos = '';
        return;
      }
      // 计算实际插入位置
      let targetIdx = idx;
      if (this.dragOverPos === 'bottom') targetIdx = idx + 1;
      // 仅在已勾选集合内重排
      const checkedArr = arr.filter(r => this.isChecked(r));
      const srcIdx = checkedArr.indexOf(src);
      if (srcIdx < 0) {
        this.dragIndex = -1;
        this.dragOverPos = '';
        return;
      }
      // 找到 targetIdx 对应 checkedArr 中的索引
      let tgtCheckedIdx;
      if (targetIdx >= arr.length) {
        tgtCheckedIdx = checkedArr.length; // 插入到末尾
      } else {
        const tgtRow = arr[targetIdx];
        tgtCheckedIdx = checkedArr.indexOf(tgtRow);
        if (tgtCheckedIdx < 0) {
          // 目标行未勾选，找下一个已勾选行
          for (let i = targetIdx; i < arr.length; i++) {
            const ci = checkedArr.indexOf(arr[i]);
            if (ci >= 0) { tgtCheckedIdx = ci; break }
          }
          if (tgtCheckedIdx < 0) tgtCheckedIdx = checkedArr.length;
        }
      }
      if (srcIdx === tgtCheckedIdx || srcIdx === tgtCheckedIdx - 1) {
        this.dragIndex = -1;
        this.dragOverPos = '';
        return;
      }
      // 重排
      const sortKey = this.currentSortKey;
      checkedArr.splice(srcIdx, 1);
      const insertIdx = srcIdx < tgtCheckedIdx ? tgtCheckedIdx - 1 : tgtCheckedIdx;
      checkedArr.splice(insertIdx, 0, src);
      checkedArr.forEach((r, i) => this._setRowField(r, sortKey, i + 1));
      this.dragIndex = -1;
      this.dragOverPos = '';
    },
    onDragEnd() {
      this.dragIndex = -1;
      this.dragOverIndex = -1;
      this.dragOverPos = '';
    },
    // 编辑类型中文标签
    editTypeLabel(type) {
      const opt = this.editTypeOptions.find(o => o.key === type);
      return opt ? opt.title : (type || '未设置');
    },
    // 表单画布：切换占宽
    toggleFormColspan(row) {
      const next = +row.COLSPAN >= 2 ? 1 : 2;
      this._setRowField(row, 'COLSPAN', next);
    },
    // 表单画布：直接设置占宽（1=半行, 2=整行）
    setColspan(row, val) {
      const cur = +row.COLSPAN >= 2 ? 2 : 1;
      if (cur !== val) this._setRowField(row, 'COLSPAN', val);
    },
    // === 设计器模式：通过 field 反查 DTSC row ===
    findRowByField(field) {
      const key = field && field.props && field.props.key;
      if (!key) return null;
      return this.DTSC.find(r => (r.RESFIELDNAME || r.FIELDNAME) === key);
    },
    fieldColspan(field) {
      const row = this.findRowByField(field);
      return row ? row.COLSPAN : 1;
    },
    setFieldColspan(field, val) {
      const row = this.findRowByField(field);
      if (!row) return;
      const cur = +row.COLSPAN >= 2 ? 2 : 1;
      if (cur !== val) this._setRowField(row, 'COLSPAN', val);
    },
    // === 设计器拖拽事件（由 rs-form-edit designer 模式 emit） ===
    onDesignerCellClick(field) {
      const row = this.findRowByField(field);
      if (row) this.currentRow = row;
    },
    onDesignerCellDragStart({ field }) {
      const key = field && field.props && field.props.key;
      this.formDrag = { key: key || '', overKey: '', overPos: '' };
    },
    onDesignerCellDragOver({ field, pos }) {
      const key = field && field.props && field.props.key;
      if (!this.formDrag.key || this.formDrag.key === key) return;
      this.formDrag.overKey = key || '';
      this.formDrag.overPos = pos;
    },
    onDesignerCellDrop({ field }) {
      const overKey = field && field.props && field.props.key;
      const srcKey = this.formDrag.key;
      const dragPos = this.formDrag.overPos; // 'before' | 'after'
      this.formDrag = { key: '', overKey: '', overPos: '' };
      if (!srcKey || srcKey === overKey) return;
      const g = this.currentFormGroup;
      if (!g) return;
      const items = g.items.slice();
      const srcIdx = items.findIndex(r => (r.RESFIELDNAME || r.FIELDNAME) === srcKey);
      if (srcIdx < 0) return;
      let tgtIdx = items.findIndex(r => (r.RESFIELDNAME || r.FIELDNAME) === overKey);
      if (tgtIdx < 0) return;
      // before=插到目标前面(同位置)，after=插到目标后面(位置+1)
      if (dragPos === 'after') tgtIdx += 1;
      if (srcIdx === tgtIdx || srcIdx === tgtIdx - 1) return;
      const src = items.splice(srcIdx, 1)[0];
      const insertIdx = srcIdx < tgtIdx ? tgtIdx - 1 : tgtIdx;
      items.splice(insertIdx, 0, src);
      // 重算该分组内所有字段的 EDITSORT
      const startSort = this.calcGroupStartSort(g.name);
      items.forEach((r, i) => {
        this._setRowField(r, 'EDITSORT', startSort + i);
      });
    },
    onDesignerCellDragLeave() {
      this.formDrag.overKey = '';
      this.formDrag.overPos = '';
    },
    onDesignerCellDragEnd() {
      this.formDrag = { key: '', overKey: '', overPos: '' };
    },
    // === 查询预览拖拽（直接在 FormItem 上拖拽，更新 QUERYSORT） ===
    onQueryDragStart(row, e) {
      const key = row.RESFIELDNAME || row.FIELDNAME;
      this.queryDrag = { key: key || '', overKey: '', overPos: '' };
      try { e.dataTransfer.effectAllowed = 'move'; } catch (err) { /* noop */ }
    },
    onQueryDragOver(row, e) {
      if (!this.queryDrag.key) return;
      const key = row.RESFIELDNAME || row.FIELDNAME;
      if (this.queryDrag.key === key) return;
      e.preventDefault();
      this.queryDrag.overKey = key || '';
      const rect = e.currentTarget.getBoundingClientRect();
      const midX = rect.left + rect.width / 2;
      this.queryDrag.overPos = e.clientX < midX ? 'before' : 'after';
    },
    onQueryDrop(row, e) {
      e.preventDefault();
      const overKey = row.RESFIELDNAME || row.FIELDNAME;
      const srcKey = this.queryDrag.key;
      const dragPos = this.queryDrag.overPos;
      this.queryDrag = { key: '', overKey: '', overPos: '' };
      if (!srcKey || srcKey === overKey) return;
      const arr = this.DTSC.filter(r => +r.QUERYSORT > 0);
      arr.sort((a, b) => +a.QUERYSORT - +b.QUERYSORT);
      const srcIdx = arr.findIndex(r => (r.RESFIELDNAME || r.FIELDNAME) === srcKey);
      if (srcIdx < 0) return;
      const tgtIdx = arr.findIndex(r => (r.RESFIELDNAME || r.FIELDNAME) === overKey);
      if (tgtIdx < 0) return;
      const insertTarget = dragPos === 'after' ? tgtIdx + 1 : tgtIdx;
      if (srcIdx === insertTarget || srcIdx === insertTarget - 1) return;
      const src = arr.splice(srcIdx, 1)[0];
      const insertIdx = srcIdx < insertTarget ? insertTarget - 1 : insertTarget;
      arr.splice(insertIdx, 0, src);
      arr.forEach((r, i) => this._setRowField(r, 'QUERYSORT', i + 1));
    },
    onQueryDragLeave(row) {
      const key = row.RESFIELDNAME || row.FIELDNAME;
      if (this.queryDrag.overKey === key) {
        this.queryDrag.overKey = '';
        this.queryDrag.overPos = '';
      }
    },
    onQueryDragEnd() {
      this.queryDrag = { key: '', overKey: '', overPos: '' };
    },
    // 计算某分组第一个字段应使用的起始 EDITSORT（按现有分组顺序累加）
    calcGroupStartSort(groupName) {
      let start = 1;
      for (let i = 0; i < this.formGroups.length; i++) {
        const g = this.formGroups[i];
        if (g.name === groupName) return start;
        start += g.items.length;
      }
      return start;
    },
    // 打开分组编辑弹窗
    openGroupEdit(mode) {
      this.groupEdit.mode = mode;
      if (mode === 'rename') {
        this.groupEdit.value = this.activeFormGroup;
      } else {
        this.groupEdit.value = '';
      }
      this.groupEdit.show = true;
    },
    saveGroupEdit() {
      const name = (this.groupEdit.value || '').trim();
      if (!name) {
        this.$error('分组名称不能为空');
        return;
      }
      if (this.groupEdit.mode === 'add') {
        if (this.formGroups.find(g => g.name === name)) {
          this.$error('分组名称已存在');
          return;
        }
        // 新增空分组：加入 customGroups（formGroups computed 会合并显示）
        this.customGroups.push(name);
        this.activeFormGroup = name;
        this.groupEdit.show = false;
      } else {
        // 重命名：把当前分组下所有字段 EDITGROUP 改过去
        const oldName = this.activeFormGroup;
        if (oldName === name) {
          this.groupEdit.show = false;
          return;
        }
        if (this.formGroups.find(g => g.name === name)) {
          this.$error('分组名称已存在');
          return;
        }
        const g = this.formGroups.find(gg => gg.name === oldName);
        if (g) {
          g.items.forEach(r => this._setRowField(r, 'EDITGROUP', name));
        }
        // 自定义空分组重命名：从 customGroups 移除旧名，加入新名
        const ci = this.customGroups.indexOf(oldName);
        if (ci >= 0) {
          this.customGroups.splice(ci, 1, name);
        }
        this.activeFormGroup = name;
        this.groupEdit.show = false;
      }
    },
    async removeCurrentGroup() {
      const name = this.activeFormGroup;
      if (!name) return;
      const g = this.formGroups.find(gg => gg.name === name);
      if (!g) return;
      if (g.items.length > 0) {
        try {
          await this.$confirm(
            `分组「${name}」内还有 ${g.items.length} 个字段，删除后这些字段将移入「基本信息」分组，是否继续？`,
            '删除分组'
          );
        } catch (e) { return; }
        g.items.forEach(r => {
          this._setRowField(r, 'EDITGROUP', '基本信息');
        });
      }
      // 从 customGroups 移除（如果是空分组）
      const ci = this.customGroups.indexOf(name);
      if (ci >= 0) this.customGroups.splice(ci, 1);
      // 切换到第一个其他分组
      const remaining = this.formGroups.filter(gg => gg.name !== name);
      this.activeFormGroup = remaining.length > 0 ? remaining[0].name : '';
    },
    removeField(row) {
      this.$store.commit(`${Constants.STORE_NAME}/DEL_DTSC`, { item: row });
      if (this.currentRow === row) this.currentRow = null;
      this.compactSort();
    },
    // 左右分栏拖拽
    onLeftResizeStart(e) {
      e.preventDefault();
      const startX = e.clientX;
      const startWidth = this.leftWidth || this.$el.querySelector('.uis-left').offsetWidth;
      const onMove = (ev) => {
        const delta = ev.clientX - startX;
        this.leftWidth = Math.max(250, Math.min(startWidth + delta, window.innerWidth * 0.7));
      };
      const onUp = () => {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    // 字段列表↔属性面板 拖拽
    onFieldResizeStart(e) {
      e.preventDefault();
      const startY = e.clientY;
      const startHeight = this.propPanelHeight;
      const onMove = (ev) => {
        const delta = startY - ev.clientY;
        this.propPanelHeight = Math.max(120, Math.min(startHeight + delta, window.innerHeight * 0.6));
      };
      const onUp = () => {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    // 动作代码弹窗
    openActionDialog() {
      if (!this.currentRow) return;
      const raw = this.currentRow.ACTIONCODE || '';
      this.actionRows = raw.split('|')
        .filter(seg => seg.trim())
        .map(seg => {
          // 格式：标签:功能点编码,模块编码/功能点编码
          // 例：页面配置:uiset,RS_M01/A03
          const [labelPart, rest] = seg.split(':');
          const label = (labelPart || '').trim();
          const [pointCode, moduleApi] = (rest || '').split(',');
          const pointCodeTrim = (pointCode || '').trim();
          const moduleApiTrim = (moduleApi || '').trim();
          // 拆分 模块编码/功能点编码
          const [funcCode, funcPointCode] = moduleApiTrim.split('/');
          const row = {
            label,
            pointCode: pointCodeTrim,
            funcCode: funcCode || '',
            funcPointCode: funcPointCode || '',
            actionPointList: [],
          };
          // 预加载该功能下的功能点列表
          if (funcCode) {
            const func = this.actionFuncOptions.find(f => f.FUNCCODE === funcCode);
            if (func) {
              row.actionPointList = this.actionFuncPointMap[func.ID] || [];
            }
          }
          return row;
        });
      if (this.actionRows.length === 0) {
        this.actionRows.push({ label: '', pointCode: '', funcCode: '', funcPointCode: '', actionPointList: [] });
      }
      this.actionDialogShow = true;
    },
    saveActionDialog() {
      const s = this.actionRows
        .filter(r => r.label)
        .map(r => {
          let code = r.label + ':' + r.pointCode;
          if (r.funcCode) {
            code += ',' + r.funcCode;
            if (r.funcPointCode) code += '/' + r.funcPointCode;
          }
          return code;
        })
        .join('|');
      this.setField('ACTIONCODE', s);
      this.actionDialogShow = false;
    },
    addActionRow() {
      this.actionRows.push({ label: '', pointCode: '', funcCode: '', funcPointCode: '', actionPointList: [] });
    },
    onActionFuncChange(row, funcCode) {
      row.funcCode = funcCode;
      row.funcPointCode = '';
      const func = this.actionFuncOptions.find(f => f.FUNCCODE === funcCode);
      if (func) {
        row.actionPointList = this.actionFuncPointMap[func.ID] || [];
      } else {
        row.actionPointList = [];
      }
    },
    addBlank() {
      const defaults = {};
      if (this.activeTab === 'list') {
        const sortKey = this.listSortField;
        let max = 0;
        this.DTSC.forEach(r => { const v = +r[sortKey] || 0; if (v > max) max = v; });
        defaults[sortKey] = max + 1;
        defaults.DISPLAYINLIST = 1;
      } else if (this.activeTab === 'form') {
        let max = 0;
        this.DTSC.forEach(r => { const v = +r.EDITSORT || 0; if (v > max) max = v; });
        defaults.EDITSORT = max + 1;
      } else if (this.activeTab === 'query') {
        let max = 0;
        this.DTSC.forEach(r => { const v = +r.QUERYSORT || 0; if (v > max) max = v; });
        defaults.QUERYSORT = max + 1;
      }
      this.$store.commit(`${Constants.STORE_NAME}/ADD_DTSC`, { RESOURCEID: this.resourceId, defaults });
    },
    // === 从其他字段复制配置 ===
    openCopyConfigPopup() {
      this.$refs.configSelPopup.openField();
    },
    onConfigSelConfirm({ mode, data }) {
      if (mode !== 'field' || !data || !this.currentRow) return;
      const copyFields = ['EDITTYPE', 'SELECTDATA', 'UPDATEFIELDS', 'QUERYTYPE', 'QUERYMODE', 'PLACEHOLDER', 'MAXLENGTH', 'COLSPAN', 'EDITGROUP'];
      let copied = [];
      copyFields.forEach(f => {
        if (data[f] !== undefined && data[f] !== null && data[f] !== '') {
          this._setRowField(this.currentRow, f, data[f]);
          copied.push(f);
        }
      });
      if (copied.length > 0) {
        this.$Message.success('已复制配置：' + copied.join(', '));
      } else {
        this.$Message.warning('该字段无可复制的配置值');
      }
    },
    openFieldSel() {
      this.fieldSelShow = true;
    },
    onSelectFields(items) {
      this.$store.commit(`${Constants.STORE_NAME}/SET_DTSC`, { items });
      // 根据当前 Tab 给新入字段初始化排序值，使其立即可见
      if (this.activeTab === 'list') {
        const sortKey = this.listSortField;
        items.forEach(item => {
          const row = this.DTSC.find(r => r.RESFIELDID === item.ID);
          if (row && !this.listDisplayOf(row) && (!row[sortKey] || +row[sortKey] === 0)) {
            let max = 0;
            this.DTSC.forEach(r => { const v = +r[sortKey] || 0; if (v > max) max = v; });
            this._setRowField(row, sortKey, max + 1);
            this._setRowField(row, 'DISPLAYINLIST', 1);
          }
        });
      } else if (this.activeTab === 'form') {
        items.forEach(item => {
          const row = this.DTSC.find(r => r.RESFIELDID === item.ID);
          if (row && (!row.EDITSORT || +row.EDITSORT === 0)) {
            let max = 0;
            this.DTSC.forEach(r => { const v = +r.EDITSORT || 0; if (v > max) max = v; });
            this._setRowField(row, 'EDITSORT', max + 1);
          }
        });
      } else if (this.activeTab === 'query') {
        items.forEach(item => {
          const row = this.DTSC.find(r => r.RESFIELDID === item.ID);
          if (row && (!row.QUERYSORT || +row.QUERYSORT === 0)) {
            let max = 0;
            this.DTSC.forEach(r => { const v = +r.QUERYSORT || 0; if (v > max) max = v; });
            this._setRowField(row, 'QUERYSORT', max + 1);
          }
        });
      }
      this.fieldSelShow = false;
    },
    parseSelectDatasFromText(text) {
      if (!text) return [];
      // 尝试 JSON
      try {
        const parsed = JSON.parse(text);
        if (Array.isArray(parsed)) return parsed;
        // {dict:..., items:[...]} 格式：从字典中过滤
        if (parsed && typeof parsed === 'object' && parsed.dict) {
          const dicts = (this.$store.state.app && this.$store.state.app.dicts) || {};
          const dict = dicts[parsed.dict] || {};
          const items = parsed.items || [];
          if (items.length > 0) {
            const keySet = new Set(items);
            return Object.keys(dict).filter(k => keySet.has(k)).map(k => ({ key: k, title: dict[k] }));
          }
          return Object.keys(dict).map(k => ({ key: k, title: dict[k] }));
        }
        // 普通对象 {key:title} -> 数组
        if (parsed && typeof parsed === 'object') {
          return Object.keys(parsed).map(k => ({ key: k, title: parsed[k] }));
        }
      } catch (e) {}
      // 尝试 k:v,k:v
      if (typeof text === 'string' && text.indexOf(':') > 0) {
        return text.split(',').map(seg => {
          const [k, title] = seg.split(':');
          return { key: (k || '').trim(), title: (title || k || '').trim() };
        });
      }
      return [];
    },
    onCancel() {
      this.$emit('close');
    },
    onSave() {
      this.saving = true;
      this.$callAction({
        action: `${Constants.STORE_NAME}/saveDTSC`,
        successText: '保存成功',
        successCall: () => {
          this.saving = false;
          this.$emit('close');
          this.$emit('saved');
        },
        errorCall: () => { this.saving = false },
      });
    },
  },
};
</script>

<style scoped lang="less">
.uis-full {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #f5f7fa;
}
.uis-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 46px;
  padding: 0 14px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.uis-title {
  display: flex;
  align-items: center;
  font-size: 15px;
}
.uis-title-label {
  color: #1d39c4;
  font-weight: 600;
  margin-right: 10px;
}
.uis-title-res {
  color: #666;
  font-size: 13px;
}
.uis-actions {
  display: flex;
}
.uis-body {
  display: flex;
  flex: 1;
  min-height: 0;
  padding: 10px;
  gap: 10px;
}
.uis-left {
  width: 25%;
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  min-width: 0;
  flex-shrink: 0;
}
.uis-right {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  min-width: 0;
}
.uis-resizer-v {
  width: 4px;
  cursor: col-resize;
  background: #e8e8e8;
  border-radius: 2px;
  flex-shrink: 0;
  transition: background .15s;
  &:hover {
    background: #1d39c4;
  }
}
.uis-resizer-h {
  height: 4px;
  cursor: row-resize;
  background: #e8e8e8;
  border-radius: 2px;
  flex-shrink: 0;
  transition: background .15s;
  &:hover {
    background: #1d39c4;
  }
}
.uis-tabs {
  display: flex;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.uis-tab {
  padding: 10px 18px;
  cursor: pointer;
  font-size: 13px;
  color: #555;
  border-bottom: 2px solid transparent;
  transition: all .15s;
  &:hover {
    color: #1d39c4;
  }
  &.active {
    color: #1d39c4;
    border-bottom-color: #1d39c4;
    font-weight: 600;
  }
}
.uis-field-list {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  border-bottom: 1px solid #f0f0f0;
}
.uis-toolbar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 10px;
  border-bottom: 1px solid #f5f5f5;
  flex-shrink: 0;
  flex-wrap: wrap;
}
.uis-toolbar-tip {
  color: #999;
  font-size: 12px;
  margin-left: 4px;
}
.uis-fields {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
}
.uis-field-row {
  position: relative;
  display: flex;
  align-items: center;
  padding: 6px 10px;
  border-bottom: 1px dashed #f0f0f0;
  cursor: pointer;
  gap: 8px;
  height: 40px;
  box-sizing: border-box;
  &:hover {
    background: #f7faff;
  }
  &.active {
    background: #eef3ff;
    border-left: 3px solid #1d39c4;
    padding-left: 7px;
  }
  &.unchecked {
    opacity: .55;
  }
  &.dragging {
    opacity: .55;
  }
  &.drag-over-top::before,
  &.drag-over-bottom::after {
    content: '';
    position: absolute;
    left: 0;
    right: 0;
    height: 3px;
    background: #1d39c4;
    border-radius: 2px;
    z-index: 1;
  }
  &.drag-over-top::before {
    top: -2px;
  }
  &.drag-over-bottom::after {
    bottom: -2px;
  }
}
.uis-field-row input[type="checkbox"] {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
}
.uis-field-name {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.uis-field-name .lbl {
  font-size: 13px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.uis-field-name .fld {
  font-size: 11px;
  color: #999;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.uis-field-ops {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}
.uis-op-btn {
  width: 22px;
  height: 22px;
  border: 1px solid #d9d9d9;
  background: #fafafa;
  border-radius: 3px;
  cursor: pointer;
  font-size: 12px;
  line-height: 20px;
  padding: 0;
  color: #555;
  &:hover {
    background: #e6f0ff;
    color: #1d39c4;
    border-color: #91d5ff;
  }
}
.uis-op-del:hover {
  background: #fff1f0;
  color: #f5222d;
  border-color: #ffa39e;
}
.uis-op-disabled {
  font-size: 11px;
  color: #bbb;
}
.uis-prop-panel {
  min-height: 120px;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}
.uis-prop-title {
  padding: 6px 10px;
  font-size: 12px;
  color: #666;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}
.uis-prop-body {
  flex: 1;
  overflow-y: auto;
  padding: 8px 12px;
}
.uis-prop-body .h-form-item {
  padding-bottom: 12px;
}
.uis-advanced-sd {
  display: flex;
  align-items: center;
  gap: 6px;
  input[type="text"] {
    flex: 1;
  }
}
.uis-prop-group {
  margin: 6px 0 4px;
  padding-left: 6px;
  font-size: 12px;
  color: #1d39c4;
  border-left: 3px solid #1d39c4;
  font-weight: 600;
}
.uis-prop-body input[type="text"],
.uis-prop-body input[type="number"],
.uis-prop-body textarea {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  font-size: 13px;
  outline: none;
  &:focus {
    border-color: #1d39c4;
  }
}
.uis-prop-body textarea {
  resize: vertical;
}
.uis-empty {
  padding: 20px;
  text-align: center;
  color: #999;
  font-size: 12px;
}
.uis-preview-title {
  padding: 10px 14px;
  font-size: 13px;
  color: #333;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
}
.uis-preview-tip {
  font-size: 11px;
  color: #999;
}
.uis-preview-body {
  flex: 1;
  overflow: auto;
  padding: 14px;
}
.uis-preview-query-btns {
  margin-top: 12px;
  text-align: right;
}
.h-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  font-size: 13px;
  outline: none;
  &:focus {
    border-color: #1d39c4;
  }
}
.uis-mock-img {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 60px;
  height: 40px;
  background: #fafafa;
  border: 1px dashed #d9d9d9;
  border-radius: 3px;
  color: #bbb;
  font-size: 11px;
}
.uis-mock-file {
  display: flex;
  align-items: center;
  gap: 8px;
}
.uis-form-canvas {
  display: flex;
  flex-direction: column;
  height: 100%;
  gap: 10px;
}
.uis-form-canvas-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
  gap: 10px;
}
.uis-form-groups {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  flex: 1;
  min-width: 0;
  align-items: center;
}
.uis-form-group-tab {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 8px 4px 10px;
  font-size: 12px;
  color: #555;
  background: #fafafa;
  border: 1px solid #e8e8e8;
  border-radius: 12px;
  cursor: pointer;
  transition: all .15s;
  &:hover {
    color: #1d39c4;
    border-color: #91d5ff;
    background: #f7fbff;
  }
  &.active {
    color: #fff;
    background: #1d39c4;
    border-color: #1d39c4;
    .uis-form-group-count {
      color: #1d39c4;
      background: #fff;
    }
  }
}
.uis-form-group-count {
  display: inline-block;
  min-width: 16px;
  padding: 0 4px;
  font-size: 11px;
  line-height: 14px;
  text-align: center;
  color: #999;
  background: #fff;
  border-radius: 8px;
}
.uis-form-groups-empty {
  font-size: 12px;
  color: #bbb;
  padding: 4px 10px;
}
.uis-group-ops {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}
.uis-form-designer {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
  background: #fafafa;
  border: 1px dashed #d9d9d9;
  border-radius: 4px;
  // 穿透到 rs-form-edit 内部的 FormItem（设计器 cell 视觉反馈）
  /deep/ .uis-form-designer-cell {
    position: relative;
    border: 1px dashed transparent;
    border-radius: 3px;
    cursor: pointer;
    transition: border-color .15s, box-shadow .15s, background .15s;
    &:hover {
      border-color: #91d5ff;
      background: rgba(24,144,255,.04);
      .uis-form-designer-tools {
        opacity: 1;
        transform: translateY(0);
      }
      .uis-form-designer-field-tag {
        opacity: 1;
      }
    }
    &.active {
      border-color: #1d39c4;
      background: rgba(29,57,196,.06);
      box-shadow: 0 0 0 2px rgba(29,57,196,.1);
      .uis-form-designer-tools {
        opacity: 1;
        transform: translateY(0);
      }
      .uis-form-designer-field-tag {
        opacity: 1;
      }
    }
    &.form-dragging {
      opacity: .4;
      border-style: dashed;
      border-color: #1d39c4;
    }
    // 插入位置指示线：在 cell 内部左右边缘显示竖线（前面=左边缘，后面=右边缘）
    // 放在 cell 内部避免被画布 overflow 截断；z-index:10 确保在 label/content 之上
    &.form-drag-over-before::before,
    &.form-drag-over-after::after {
      content: '';
      position: absolute;
      top: 0;
      bottom: 0;
      width: 4px;
      background: #1d39c4;
      z-index: 10;
      box-shadow: 0 0 0 2px rgba(255,255,255,.9), 0 0 6px rgba(29,57,196,.5);
    }
    &.form-drag-over-before::before {
      left: 0;
    }
    &.form-drag-over-after::after {
      right: 0;
    }
  }
}
// 查询预览拖拽 cell 视觉反馈
.uis-query-drag-cell {
  position: relative;
  border: 1px dashed transparent;
  border-radius: 3px;
  cursor: grab;
  transition: border-color .15s, background .15s;
  &:hover {
    border-color: #91d5ff;
    background: rgba(24,144,255,.04);
  }
  &.query-dragging {
    opacity: .4;
    border-style: dashed;
    border-color: #1d39c4;
  }
  &.query-drag-over-before::before,
  &.query-drag-over-after::after {
    content: '';
    position: absolute;
    top: 0;
    bottom: 0;
    width: 4px;
    background: #1d39c4;
    z-index: 10;
    box-shadow: 0 0 0 2px rgba(255,255,255,.9), 0 0 6px rgba(29,57,196,.5);
  }
  &.query-drag-over-before::before {
    left: 0;
  }
  &.query-drag-over-after::after {
    right: 0;
  }
}
.uis-form-designer-tools {
  position: absolute;
  top: 2px;
  right: 2px;
  display: flex;
  align-items: center;
  gap: 1px;
  padding: 2px;
  background: rgba(255,255,255,0.96);
  border: 1px solid #e8e8e8;
  border-radius: 3px;
  opacity: 0;
  transform: translateY(-4px);
  transition: opacity .15s, transform .15s;
  pointer-events: auto;
  z-index: 2;
  box-shadow: 0 1px 4px rgba(0,0,0,.06);
}
.uis-form-designer-handle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  font-size: 13px;
  color: #999;
  cursor: move;
  user-select: none;
  &:hover {
    color: #1d39c4;
  }
}
.uis-form-designer-tool-btn {
  width: 22px;
  height: 18px;
  border: 1px solid transparent;
  background: transparent;
  border-radius: 2px;
  cursor: pointer;
  font-size: 11px;
  line-height: 16px;
  padding: 0;
  color: #666;
  &:hover {
    background: #e6f0ff;
    color: #1d39c4;
  }
  &.active {
    background: #1d39c4;
    color: #fff;
  }
}
.uis-form-designer-field-tag {
  position: absolute;
  bottom: 1px;
  right: 4px;
  font-size: 10px;
  color: #999;
  opacity: 0;
  transition: opacity .15s;
  pointer-events: none;
  font-family: 'Courier New', monospace;
  background: rgba(255,255,255,0.8);
  padding: 0 3px;
  border-radius: 2px;
}
.uis-form-designer-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fafafa;
  border: 1px dashed #d9d9d9;
  border-radius: 4px;
}
.uis-form-designer-empty-inner {
  text-align: center;
  color: #bbb;
}
.uis-form-designer-empty-icon {
  font-size: 32px;
  color: #d9d9d9;
  margin-bottom: 8px;
}
.uis-form-designer-empty-text {
  font-size: 13px;
  color: #999;
  margin-bottom: 4px;
}
.uis-form-designer-empty-tip {
  font-size: 11px;
  color: #bbb;
}
</style>
