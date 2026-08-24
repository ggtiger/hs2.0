<template>
  <div class="mod-config-page" ref="configPage">
    <!-- 顶部标题栏 -->
    <div class="mod-config-toolbar" v-if="!hideToolbar">
      <div class="mod-config-toolbar-left">
        <span class="mod-title-label">页面配置</span>
        <span class="mod-config-title">{{ moduleName+'['+ moduleCode+']' }}</span>
      </div>
      <div class="mod-config-toolbar-right">
        <Button size="s" icon="h-icon-plus" color="green" @click="openWizard">新建模块</Button>
        <Button size="s" icon="h-icon-setting" @click="openModuleStore">Store扩展</Button>
        <Button size="s" icon="h-icon-time" @click="openVersions">版本历史</Button>
        <Button size="s" icon="h-icon-code" @click="openCodeFiles">模块脚本</Button>
        <Button size="s" icon="h-icon-link" @click="openScriptFlowEditor">编排接口</Button>
        <Button size="s" icon="h-icon-export" v-per="'RS_M25/A05'" @click="openExportTpl">导出模板</Button>
        <Button size="s" color="cyan" icon="h-icon-share" @click="openPublishModal">发布</Button>
      </div>
    </div>

    <!-- 模块创建向导（AI 分步生成）-->
    <rs-modal ref="wizardModal" :fullScreen="true" style="z-index:9999">
      <module-wizard v-if="wizardVisible" @close="closeWizard" @done="onWizardDone" />
    </rs-modal>

    <!-- 主体: 左右布局 -->
    <div class="mod-config-body">
      <!-- 左侧面板 -->
      <div class="mod-config-left" :style="leftWidth ? { width: leftWidth + 'px' } : {}">
        <!-- 页面列表 -->
        <ToolBar label="页面配置" :size="14">
          <div slot="right" style="display:flex;align-items:center;gap:8px;float:right;">
            <Select size="s" :datas="pageTplOptions" v-model="selectedTpl" style="width:100px" placeholder="选择模板" />
            <Button color="primary" size="s" icon="h-icon-plus" @click="addPage">新增</Button>
          </div>
        </ToolBar>
        <div class="mc-page-list">
          <template v-for="page in pages">
            <div
              :key="page._idx_"
              class="mc-page-item"
              :class="{ 'mc-page-item-active': selectedIdx === page._idx_ }"
              @click="selectPage(page)"
            >
              <span class="mc-page-icon" :class="'mc-pt-' + page.PAGETYPE">{{ ptIcon(page.PAGETYPE) }}</span>
              <span class="mc-page-name">{{ page.PAGENAME || page.PAGECODE || '(未命名)' }}</span>
              <span class="mc-page-type-tag">{{ page.PAGETYPE }}</span>
              <Poptip content="确定删除该页面？" @confirm="removePage(page)" @click.native.stop>
                <Button size="s" icon="h-icon-trash" class="mc-page-del"></Button>
              </Poptip>
            </div>
            <!-- 第一层子页面 -->
            <template v-if="page._idx_ === selectedIdx && getSubPagesOf(page).length > 0">
              <div
                v-for="(sp, spIdx) in getSubPagesOf(page)"
                :key="'sp_' + spIdx"
                class="mc-subpage-item mc-subpage-l1"
                :class="{ 'mc-subpage-item-active': selectedSubIdx === spIdx && selectedSubIdx2 === null }"
                @click.stop="selectSubPage(spIdx)"
              >
                <span class="mc-subpage-icon">↳</span>
                <span class="mc-subpage-name">{{ sp.PAGENAME || '未命名' }}</span>
                <span class="mc-subpage-tag">{{ sp.REFMODULECODE ? '引用' : '自定义' }}</span>
                <span class="mc-subpage-tag">{{ sp.PAGETYPE || 'form' }}</span>
                <Poptip content="确定删除？" @confirm="removeSubPage(spIdx)" @click.native.stop>
                  <span class="mc-btn-act mc-btn-act-del mc-subpage-del"><i class="h-icon-trash"></i></span>
                </Poptip>
              </div>
              <!-- 第二层子页面（嵌套） -->
              <template v-if="selectedSubIdx !== null && getSubPagesOfSub(selectedSubIdx).length > 0">
                <div
                  v-for="(sp2, spIdx2) in getSubPagesOfSub(selectedSubIdx)"
                  :key="page._idx_ + '_sp2_' + (sp2.ID || spIdx2)"
                  class="mc-subpage-item mc-subpage-l2"
                  :class="{ 'mc-subpage-item-active': selectedSubIdx2 === spIdx2 }"
                  @click.stop="selectSubPage2(spIdx2)"
                >
                  <span class="mc-subpage-icon">↳</span>
                  <span class="mc-subpage-name">{{ sp2.PAGENAME || '未命名' }}</span>
                  <span class="mc-subpage-tag">{{ sp2.REFMODULECODE ? '引用' : '自定义' }}</span>
                  <Poptip content="确定删除？" @confirm="removeSubPage2(selectedSubIdx, spIdx2)" @click.native.stop>
                    <span class="mc-btn-act mc-btn-act-del mc-subpage-del"><i class="h-icon-trash"></i></span>
                  </Poptip>
                </div>
              </template>
            </template>
          </template>
          <div v-if="pages.length === 0" class="mc-empty">暂无页面，点击上方按钮新增</div>
        </div>

        <!-- 子页面操作按钮 -->
        <div class="mc-subpage-actions" v-if="currentPage">
          <Button size="s" icon="h-icon-plus" @click="openAddSubPage">自定义子页面</Button>
          <Button size="s" icon="h-icon-link" @click="openRefSubPage">引用其他模块</Button>
        </div>

        <!-- 下半区: 属性 + 按钮各占一半 -->
        <div class="mc-bottom" v-if="activePageConfig">
          <!-- 属性编辑 -->
          <div class="mc-half" :style="propsHeight ? { height: propsHeight + 'px', flex: 'none' } : {}">
            <ToolBar :label="isSubPageReadonly ? '页面属性（只读）' : '页面属性'" :size="14">
              <div slot="right" v-if="selectedSubIdx !== null">
                <Button size="s" icon="h-icon-edit" @click="openEditSubPage(selectedSubIdx)">编辑子页面</Button>
              </div>
            </ToolBar>
            <div class="mc-half-body">
              <Form :label-width="80" class="mc-props-form">
                <FormItem label="页面编码" single>
                  <input type="text" :value="activePageConfig.PAGECODE" :disabled="isSubPageReadonly" @input="setActivePageField('PAGECODE', $event.target.value)" placeholder="如 main" />
                </FormItem>
                <FormItem label="页面名称" single>
                  <input type="text" :value="activePageConfig.PAGENAME" :disabled="isSubPageReadonly" @input="setActivePageField('PAGENAME', $event.target.value)" placeholder="如 列表页" />
                </FormItem>
                <FormItem label="页面类型" single>
                  <Select :value="activePageConfig.PAGETYPE" :disabled="isSubPageReadonly" @input="setActivePageField('PAGETYPE', $event)" :datas="pageTypeOptions" />
                </FormItem>
                <FormItem label="路由路径" single>
                  <input type="text" :value="activePageConfig.ROUTEPATH" :disabled="isSubPageReadonly" @input="setActivePageField('ROUTEPATH', $event.target.value)" placeholder="如 /b01/m01/main" />
                </FormItem>
                <FormItem label="组件类型" single>
                  <Select :value="activePageConfig.COMPONENTTYPE" :disabled="isSubPageReadonly" @input="setActivePageField('COMPONENTTYPE', $event)" :datas="compTypeOptions" />
                </FormItem>
                <FormItem label="SFC路径" single v-if="activePageConfig.COMPONENTTYPE === 'sfc'">
                  <div style="display:flex;align-items:center;gap:6px;">
                    <span class="mc-sfc-path-tag" v-if="activePageConfig.SFCMODULEPATH">{{ activePageConfig.SFCMODULEPATH }}</span>
                    <span v-else class="mc-sfc-path-empty">未配置</span>
                    <Button size="s" icon="h-icon-edit" :disabled="isSubPageReadonly" @click="openSfcEditor('sfcmodulepath')">编辑</Button>
                    <Button size="s" icon="h-icon-trash" v-if="activePageConfig.SFCMODULEPATH && !isSubPageReadonly" @click="setActivePageField('SFCMODULEPATH', '')">清除</Button>
                  </div>
                </FormItem>
                <FormItem label="查询接口" single v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfig.QUERYAPICODE" :disabled="isSubPageReadonly" @input="setActivePageField('QUERYAPICODE', $event)" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" />
                </FormItem>
                <FormItem label="高级查询" single v-if="(activePageConfig.PAGETYPE === 'list' || activePageConfig.PAGETYPE === 'select') && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfig.ADVQUERYAPICODE" :disabled="isSubPageReadonly" @input="setActivePageField('ADVQUERYAPICODE', $event)" :datas="apiOptions" placeholder="默认同查询接口" :filterable="true" />
                </FormItem>
                <FormItem label="打开接口" single v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfig.OPENAPICODE" :disabled="isSubPageReadonly" @input="setActivePageField('OPENAPICODE', $event)" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" />
                </FormItem>
                <FormItem label="保存接口" single v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfig.SAVEAPICODE" :disabled="isSubPageReadonly" @input="setActivePageField('SAVEAPICODE', $event)" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" />
                </FormItem>
                <div class="mc-props-divider" v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">路径配置</div>
                <FormItem label="列表PATH" single v-if="(activePageConfig.PAGETYPE === 'list' || activePageConfig.PAGETYPE === 'select') && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfigJson.QRYPATH || 'QRY'" :disabled="isSubPageReadonly" @input="setActivePageConfigField('QRYPATH', $event === 'QRY' ? '' : $event)" :datas="modPathOptions" placeholder="默认QRY" />
                </FormItem>
                <FormItem label="查询PATH" single v-if="(activePageConfig.PAGETYPE === 'list' || activePageConfig.PAGETYPE === 'select') && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfigJson.QQRYSPATH || 'QQRY'" :disabled="isSubPageReadonly" @input="setActivePageConfigField('QQRYSPATH', $event === 'QQRY' ? '' : $event)" :datas="modPathOptions" placeholder="默认QQRY" />
                </FormItem>
                <FormItem label="选择模式" single v-if="activePageConfig.PAGETYPE === 'select' && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfigJson.SELECTMODE || 'single'" :disabled="isSubPageReadonly" @input="setActivePageConfigField('SELECTMODE', $event)" :datas="[{ key: 'single', title: '单选' }, { key: 'multiple', title: '多选' }]" />
                </FormItem>
                <FormItem label="表单PATH" single v-if="activePageConfig.PAGETYPE === 'form' && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfigJson.MAINPATH || 'MAIN'" :disabled="isSubPageReadonly" @input="setActivePageConfigField('MAINPATH', $event === 'MAIN' ? '' : $event)" :datas="modPathOptions" placeholder="默认MAIN" />
                </FormItem>
                <FormItem label="默认表单" single v-if="activePageConfig.PAGETYPE === 'list' && activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <Select :value="activePageConfigJson.defaultFormPageCode || ''" :disabled="isSubPageReadonly" @input="setActivePageConfigField('defaultFormPageCode', $event)" :datas="formPageOptions" placeholder="留空=第一个子form" :filterable="true" />
                  <div class="mc-quick-tags">
                    <span class="mc-quick-tag" style="color:#999">行双击/未指定formPageCode的按钮弹此页面</span>
                  </div>
                </FormItem>
                <FormItem label="排序号" single>
                  <input type="number" :value="activePageConfig.SORTNO" :disabled="isSubPageReadonly" @input="setActivePageField('SORTNO', +$event.target.value)" />
                </FormItem>
                <div class="mc-props-divider" v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">扩展</div>
                <div class="mc-slot-row" v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">
                  <span class="mc-slot-label">扩展JS</span>
                  <div class="mc-slot-value">
                    <span class="mc-sfc-path-tag" v-if="activePageConfigJson.EXTENDJS" style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">{{ activePageConfigJson.EXTENDJS }}</span>
                    <span v-else class="mc-sfc-path-empty">未配置</span>
                    <span class="mc-btn-act" :class="{'mc-btn-act-disabled': isSubPageReadonly}" @click="!isSubPageReadonly && openExtendJs()" title="编辑"><i class="h-icon-edit"></i></span>
                    <Poptip content="确定清除？" v-if="activePageConfigJson.EXTENDJS && !isSubPageReadonly" @confirm="setActivePageConfigField('EXTENDJS', '')">
                      <span class="mc-btn-act mc-btn-act-del" title="清除"><i class="h-icon-trash"></i></span>
                    </Poptip>
                  </div>
                </div>
                <template v-if="activePageConfig.COMPONENTTYPE !== 'sfc' && availableSlotNames.length > 0">
                  <div class="mc-props-divider">SFC Slot 扩展</div>
                  <div class="mc-slot-row" v-for="slotName in availableSlotNames" :key="slotName">
                    <span class="mc-slot-label" :title="slotLabel(slotName)">{{slotLabel(slotName)}}</span>
                    <div class="mc-slot-value">
                      <span class="mc-sfc-path-tag" v-if="getSlotPath(slotName)" style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">{{ getSlotPath(slotName) }}</span>
                      <span v-else class="mc-sfc-path-empty">未配置</span>
                      <span class="mc-btn-act" :class="{'mc-btn-act-disabled': isSubPageReadonly}" @click="!isSubPageReadonly && openSfcEditor('slot', slotName)" title="编辑"><i class="h-icon-edit"></i></span>
                      <Poptip content="确定清除？" v-if="getSlotPath(slotName) && !isSubPageReadonly" @confirm="setSlotPath(slotName, '')">
                        <span class="mc-btn-act mc-btn-act-del" title="清除"><i class="h-icon-trash"></i></span>
                      </Poptip>
                    </div>
                  </div>
                </template>
              </Form>
            </div>
          </div>

          <!-- 上下分隔条 -->
          <div class="mc-resizer-h" :class="{'mc-resizing': isResizing}" @mousedown="onPropsResizeStart"></div>

          <!-- 按钮配置 (SFC 自包含页面无需配按钮) -->
          <div class="mc-half" v-if="activePageConfig.COMPONENTTYPE !== 'sfc'">
            <ToolBar :label="isSubPageReadonly ? '按钮配置（只读）' : '按钮配置'" :size="14">
              <div slot="right" v-if="!isSubPageReadonly">
                <Button v-if="flowCode" size="s" icon="h-icon-complete" @click="applyFlowButtons" title="根据流程自动补充审批流按钮">按流程补按钮</Button>
                <Button size="s" icon="h-icon-copy" @click="openCopyBtnPopup">从其他模块复制</Button>
                <Button color="primary" size="s" icon="h-icon-plus" @click="openAddBtnModal">新增</Button>
              </div>

            </ToolBar>
            <div class="mc-half-body mc-btn-body">
              <!-- 统一按区域渲染: header/footer/row + 各子表路径(如 DTSA/DTSB) -->
              <template v-for="area in btnAreas">
                <div class="mc-btn-group" v-if="activeButtonsByArea(area).length > 0" :key="area">
                  <div class="mc-btn-group-title">
                    {{ isSubArea(area) ? '子表：' + area : area }}
                    <Button v-if="!isSubPageReadonly" color="primary" size="s" icon="h-icon-plus" @click="openAddBtnModal({BTNAREA: area})">新增</Button>
                  </div>
                  <div
                    v-for="(btn, bIdx) in activeButtonsByArea(area)"
                    :key="btn._idx_ || btn.ID"
                    class="mc-btn-row"
                  >
                    <span class="mc-btn-name">{{ btn.BTNNAME || btn.APICODE || '(未命名)' }}</span>
                    <span class="mc-btn-tag mc-btn-tag-btntype">{{ btn.BTNTYPE }}</span>
                    <span class="mc-btn-tag mc-btn-tag-btncode" v-if="btn.BTNCODE">{{ btn.BTNCODE }}</span>
                    <span class="mc-btn-tag mc-btn-tag-api" v-if="btn.APICODE">{{ btn.APICODE }}</span>
                    <template v-if="!isSubPageReadonly">
                      <span class="mc-btn-act" :class="{'mc-btn-act-disabled': bIdx === 0}" @click="moveBtnInArea(btn, -1)" title="上移"><i class="h-icon-top"></i></span>
                      <span class="mc-btn-act" :class="{'mc-btn-act-disabled': bIdx === activeButtonsByArea(area).length - 1}" @click="moveBtnInArea(btn, 1)" title="下移"><i class="h-icon-down"></i></span>
                      <span class="mc-btn-act" @click="openEditBtnModal(btn)" title="编辑"><i class="h-icon-setting"></i></span>
                      <Poptip content="确定删除？" @confirm="removeBtn(btn)" @click.native.stop>
                        <span class="mc-btn-act mc-btn-act-del" title="删除"><i class="h-icon-trash"></i></span>
                      </Poptip>
                    </template>
                  </div>
                </div>
              </template>
              <div v-if="activeButtons.length === 0" class="mc-empty">暂无按钮</div>
            </div>
          </div>
        </div>
      </div>

      <!-- 左右分隔条 -->
      <div class="mc-resizer-v" :class="{'mc-resizing': isResizing}" @mousedown="onLeftResizeStart"></div>

      <!-- 右侧预览 -->
      <div class="mod-config-right">
        <page-preview
          ref="pagePreview"
          :pageConfig="activePageConfig"
          :moduleCode="activeModuleCode"
          @open-ui-set="openUiSet"
        ></page-preview>
      </div>
    </div>

      <!-- 按钮新增/编辑弹窗 -->
      <rs-modal ref="btnModal" v-model="btnModalVisible" :width="460">
        <view-dialog :title="btnModalTitle" class="d-width">
          <template slot="body">
        <!-- 新增模式：先选模板 -->
        <div v-if="btnModalMode === 'add'" class="mc-btn-tpl-section">
          <div class="mc-btn-tpl-label">选择按钮模板（可选）：</div>
          <div class="mc-btn-tpl-grid">
            <div v-for="tpl in btnTemplates" :key="tpl.BTNCODE" class="mc-btn-tpl-item" @click="applyBtnTemplate(tpl)">
              <i :class="tpl.ICON || ''"></i>
              <span>{{tpl.BTNNAME}}</span>
            </div>
          </div>
          <div class="mc-btn-tpl-divider">或手动填写：</div>
        </div>
        <Form :label-width="80">
          <FormItem label="按钮名称" single>
            <input type="text" v-model="btnForm.BTNNAME" placeholder="如 新增" />
          </FormItem>
          <FormItem label="按钮区域" single>
            <Select v-model="btnForm.BTNAREA" :datas="btnAreaOptions" />
          </FormItem>
          <FormItem label="交互类型" single>
            <Select v-model="btnForm.INTERACTTYPE" :datas="btnInteractOptions" />
          </FormItem>
          <FormItem label="接口编码" single>
            <Select v-model="btnForm.APICODE" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" @input="onApicodeChange" />
          </FormItem>
          <FormItem label="权限编码" single>
            <input type="text" v-model="btnForm.PERMCODE" placeholder="自动生成，可修改" />
          </FormItem>
          <FormItem label="颜色" single>
            <Select v-model="btnForm.COLOR" :datas="btnColorOptions" />
          </FormItem>
          <FormItem label="图标" single>
            <input type="text" v-model="btnForm.ICON" placeholder="如 h-icon-plus" />
            <div class="mc-quick-tags" v-if="btnIconQuick.length">
              <span v-for="t in btnIconQuick" :key="t.key" class="mc-quick-tag" @click="btnForm.ICON = t.key"><i :class="t.key"></i> {{t.title}}</span>
            </div>
          </FormItem>
          <FormItem label="确认提示" single v-if="btnForm.INTERACTTYPE==='poptip'">
            <input type="text" v-model="btnForm.POPTIPTEXT" placeholder="如 确定删除？" />
            <div class="mc-quick-tags">
              <span v-for="t in btnPoptipOptions" :key="t.key" class="mc-quick-tag" @click="btnForm.POPTIPTEXT = t.key">{{t.title}}</span>
            </div>
          </FormItem>
          <FormItem label="显隐条件" single>
            <input type="text" v-model="btnForm.SHOWCOND" placeholder="如 STATE===1 或 ID!=null，或方法名 ISSHOWXXX" />
            <div class="mc-quick-tags">
              <span v-for="t in btnShowCondOptions" :key="t.key" class="mc-quick-tag" @click="btnForm.SHOWCOND = t.key">{{t.title}}</span>
              <Button size="s" icon="h-icon-code" @click="openHookEditor('showCond')" class="mc-quick-btn">插入方法</Button>
            </div>
          </FormItem>
          <div class="mc-props-divider">动作配置</div>
          <FormItem label="按钮编码" single>
            <input type="text" v-model="btnForm.BTNCODE" placeholder="点击下方标签选择预设，或直接输入自定义编码" />
            <div class="mc-quick-tags">
              <span v-for="t in btnCodeOptions" :key="t.key" class="mc-quick-tag" @click="selectBtncode(t.key)">{{t.title}}</span>
            </div>
          </FormItem>
          <FormItem label="动作类型" single>
            <Select v-model="btnForm.ACTIONTYPE" :datas="[{key:'api',title:'api(调用接口)'},{key:'openForm',title:'openForm(打开表单)'},{key:'openSelector',title:'openSelector(选入)'}]" />
          </FormItem>
          <!-- api 配置 -->
          <FormItem label="接口编码" single v-if="btnForm.ACTIONTYPE === 'api'">
            <Select v-model="btnForm.APICODE" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" @input="onApicodeChange" />
          </FormItem>
          <!-- openForm 配置 -->
          <template v-if="btnForm.ACTIONTYPE === 'openForm'">
            <FormItem label="接口编码" single>
              <Select v-model="btnForm.APICODE" :datas="apiOptions" placeholder="-- 选择 --" :filterable="true" @input="onApicodeChange" />
            </FormItem>
            <FormItem label="打开模式" single>
              <Select v-model="btnForm.OPENMODE" :datas="[{key:'add',title:'新增(空表单)'},{key:'edit',title:'编辑(当前行)'}]" />
            </FormItem>
            <FormItem label="目标页面" single>
              <Select v-model="btnForm.FORMPAGECODE" :datas="formPageOptions" placeholder="留空=默认表单" :filterable="true" />
            </FormItem>
            <FormItem label="弹窗宽度" single>
              <input type="number" v-model.number="btnForm.MODALWIDTH" placeholder="留空=默认" />
            </FormItem>
            <FormItem label="全屏弹窗" single>
              <h-switch v-model="btnForm.MODALFULLSCREEN" :trueValue="true" :falseValue="false" />
            </FormItem>
          </template>
          <!-- openSelector 配置 -->
          <template v-if="btnForm.ACTIONTYPE === 'openSelector'">
            <FormItem label="选择模式" single>
              <Select v-model="btnForm.SELECTMODE" :datas="[{key:'single',title:'单选'},{key:'multiple',title:'多选'}]" />
            </FormItem>
            <FormItem label="选入页面" single>
              <Select v-model="btnForm.SELECTPAGECODE" :datas="selectPageOptions" placeholder="选择 select 页面" :filterable="true" />
            </FormItem>
            <FormItem label="选入目标" single>
              <Select v-model="btnForm.SELECTTARGET" :datas="selectTargetOptions" placeholder="写入的子表路径" :filterable="true" />
            </FormItem>
            <FormItem label="字段映射" single>
              <div style="display:flex;align-items:center;gap:8px;">
                <input type="text" v-model="btnForm.FIELDMAP" readonly placeholder="点击配置按钮设置字段映射" style="flex:1;cursor:pointer;background:#f9f9f9;" @click="openFieldMapModal" />
                <Button size="s" icon="h-icon-setting" @click="openFieldMapModal">配置</Button>
              </div>
              <div class="mc-quick-tags">
                <span class="mc-quick-tag" style="color:#999">选入行字段=子表字段，逗号分隔</span>
              </div>
            </FormItem>
            <FormItem label="选入宽度" single>
              <input type="number" v-model.number="btnForm.SELECTWIDTH" placeholder="默认 900" />
            </FormItem>
          </template>
          <FormItem label="前置钩子" single>
            <input type="text" v-model="btnForm.BEFOREACTION" placeholder="扩展JS方法名，如 beforeSave" />
            <div class="mc-quick-tags">
              <span class="mc-quick-tag" style="color:#999">点击前调用，返回false中止</span>
              <Button size="s" icon="h-icon-code" @click="openHookEditor('beforeAction')" class="mc-quick-btn">插入方法</Button>
            </div>
          </FormItem>
          <FormItem label="后置钩子" single>
            <input type="text" v-model="btnForm.AFTERACTION" placeholder="扩展JS方法名，如 afterSave" />
            <div class="mc-quick-tags">
              <span class="mc-quick-tag" style="color:#999">动作完成后调用</span>
              <Button size="s" icon="h-icon-code" @click="openHookEditor('afterAction')" class="mc-quick-btn">插入方法</Button>
            </div>
          </FormItem>
          <FormItem label="额外参数" single>
            <input type="text" v-model="btnForm.EXTRAPARAMS" placeholder='JSON 如 {"STATE":"1"}；或填方法名取动态参数' />
            <div class="mc-quick-tags">
              <span class="mc-quick-tag" style="color:#999">JSON静态参数 或 方法名动态参数</span>
              <Button size="s" icon="h-icon-code" @click="openHookEditor('paramsFn')" class="mc-quick-btn">插入方法</Button>
            </div>
          </FormItem>
          <FormItem label="排序号" single>
            <input type="number" v-model.number="btnForm.SORTNO" />
          </FormItem>
        </Form>
          </template>
          <template slot="footer">
            <Button @click="btnModalVisible = false">取消</Button>
            <Button color="primary" @click="confirmBtnModal">确定</Button>
          </template>
        </view-dialog>
      </rs-modal>

      <!-- 子页面配置弹窗 -->
      <rs-modal ref="subPageModal" v-model="subPageModalVisible" :width="520">
        <view-dialog :title="subPageModalMode === 'add' ? '新增子页面' : subPageModalMode === 'ref' ? '引用模块页面' : '编辑子页面'" class="d-width">
          <template slot="body">
            <Form :label-width="90">
          <!-- 引用模式：模块+页面选择 -->
          <template v-if="subPageModalMode === 'ref'">
            <FormItem label="目标模块" single>
              <Select v-model="subPageForm.REFMODULECODE" :datas="refModuleOptions" placeholder="选择模块" :filterable="true" @input="loadRefPages" />
            </FormItem>
            <FormItem label="目标页面" single>
              <Select v-model="subPageForm.REFPAGECODE" :datas="refPageOptions" placeholder="选择页面" :filterable="true" @input="onRefPageChange" />
            </FormItem>
          </template>
          <!-- 自定义模式：页面名称 -->
          <FormItem label="页面名称" single>
            <input type="text" v-model="subPageForm.PAGENAME" placeholder="子页面显示名称" />
          </FormItem>
          <FormItem label="页面编码" single v-if="subPageModalMode !== 'ref'">
            <input type="text" v-model="subPageForm.PAGECODE" :placeholder="subPageModalMode === 'add' ? '自动生成' : '如 sub_form'" />
          </FormItem>
          <FormItem label="页面类型" single v-if="subPageModalMode !== 'ref'">
            <Select v-model="subPageForm.PAGETYPE" :datas="pageTypeOptions" />
          </FormItem>
          <FormItem label="组件类型" single v-if="subPageModalMode !== 'ref'">
            <Select v-model="subPageForm.COMPONENTTYPE" :datas="compTypeOptions" />
          </FormItem>
          <FormItem label="SFC路径" single v-if="subPageModalMode !== 'ref' && subPageForm.COMPONENTTYPE === 'sfc'">
            <div style="display:flex;align-items:center;gap:6px;">
              <span class="mc-sfc-path-tag" v-if="subPageForm.SFCMODULEPATH">{{ subPageForm.SFCMODULEPATH }}</span>
              <span v-else class="mc-sfc-path-empty">未配置</span>
              <Button size="s" icon="h-icon-edit" @click="openSfcEditor('sub_sfcmodulepath')">编辑</Button>
              <Button size="s" icon="h-icon-trash" v-if="subPageForm.SFCMODULEPATH" @click="subPageForm.SFCMODULEPATH = ''">清除</Button>
            </div>
          </FormItem>
          <!-- 弹窗配置 -->
          <div class="mc-props-divider">弹窗配置</div>
          <FormItem label="弹窗宽度" single>
            <input type="number" v-model.number="subPageForm.MODALWIDTH" placeholder="默认宽度" />
          </FormItem>
          <FormItem label="全屏弹窗" single>
            <h-switch v-model="subPageForm.MODALFULLSCREEN" :trueValue="true" :falseValue="false" />
          </FormItem>
        </Form>
          </template>
          <template slot="footer">
            <Button @click="subPageModalVisible = false">取消</Button>
            <Button color="primary" @click="confirmSubPageModal">确定</Button>
          </template>
        </view-dialog>
      </rs-modal>

    <!-- uiSetFull 全屏弹窗 -->
    <Modal v-model="uiSetFullShow" :title="uiSetFullTitle" fullScreen hasCloseIcon>
      <ui-set-full
        v-if="uiSetFullShow"
        ref="uiSetFull"
        :resourceId="uiSetResourceId"
        :resourceName="uiSetResourceName"
        @close="uiSetFullShow = false"
        @saved="onUiSetSaved"
        @saving-change="uiSetFullSaving = $event"
      ></ui-set-full>
      <div slot="footer">
        <Button @click="uiSetFullShow = false">取消</Button>
        <Button color="primary" class="ml5" :loading="uiSetFullSaving" @click="onUiSetSave">保存</Button>
      </div>
    </Modal>

    <!-- SFC 编辑器弹窗 -->
    <sfc-editor-popup
      ref="sfcEditor"
      :title="sfcEditorTitle"
      @saved="onSfcEditorSaved"
    ></sfc-editor-popup>

    <!-- 发布到菜单弹窗 -->
    <rs-modal ref="publishModal" v-model="publishModalVisible" :width="520">
      <view-dialog title="发布模块到菜单" class="d-width">
        <template slot="body">
          <div style="padding: 16px 20px;">
            <Form :label-width="90" mode="single">
              <FormItem label="模块编码">
                <div style="padding-top:6px;color:#555;">{{ moduleCode }}</div>
              </FormItem>
              <FormItem label="菜单名称">
                <input v-model="publishForm.funcName" placeholder="菜单显示名称" class="publish-input" />
              </FormItem>
              <FormItem label="发布目标">
                <TreePicker
                  :option="publishTreeOption"
                  ref="publishTarget"
                  v-model="publishForm.targetId"
                  style="width:100%"
                ></TreePicker>
              </FormItem>
              <div class="publish-tip">
                <i class="h-icon-info" style="margin-right:4px;"></i>
                选择<span class="publish-em">目录</span> = 在该目录下新增菜单；
                选择<span class="publish-em">已有模块</span> = 替换该模块的 URL / 功能编码
              </div>
            </Form>
          </div>
        </template>
        <template slot="footer">
          <Button @click="publishModalVisible = false">取消</Button>
          <Button color="primary" :loading="publishing" @click="handlePublish">发布</Button>
        </template>
      </view-dialog>
    </rs-modal>
    <!-- 代码资产编辑器弹窗（模块脚本入口直接打开，左侧列表限定当前模块相关资产） -->
    <code-editor-popup ref="codeEditorPopup" @saved="onCodeAssetSaved" />
    <script-flow-editor ref="scriptFlowEditor" @saved="onScriptFlowSaved" />
    <config-sel-popup ref="configSelPopup" @confirm="onConfigSelConfirm" />
    <!-- 导出为模板弹窗 -->
    <rs-modal ref="exportTplModal" v-model="exportTplModalVisible" :width="520">
      <view-dialog title="导出为业务模板" class="d-width">
        <template slot="body">
          <div style="padding: 16px 20px;">
            <Form :label-width="90" mode="single">
              <FormItem label="来源模块">
                <div style="padding-top:6px;color:#555;">{{ moduleName }}（{{ moduleCode }}）</div>
              </FormItem>
              <FormItem label="模板编码" required>
                <input v-model="exportTplForm.templateCode" placeholder="如 TPL_LOGISTICS" class="publish-input" />
              </FormItem>
              <FormItem label="模板名称" required>
                <input v-model="exportTplForm.templateName" placeholder="如 物流管理模板" class="publish-input" />
              </FormItem>
              <FormItem label="业务分类">
                <Select v-model="exportTplForm.category" :datas="bizCatOptions"></Select>
              </FormItem>
              <FormItem label="描述">
                <textarea v-model="exportTplForm.description" placeholder="模板用途说明（可选）" class="publish-input" style="height:60px;"></textarea>
              </FormItem>
              <div class="publish-tip">
                <i class="h-icon-info" style="margin-right:4px;"></i>
                导出范围：模块的数据源/字段/过滤器/UI/接口/页面/按钮/菜单/权限及引用的 SQL 模板、字典、SFC 文件。
                安装时在模板市场填写变量（新模块编码/名称/父菜单）即可一键复用。
              </div>
            </Form>
          </div>
        </template>
        <template slot="footer">
          <Button @click="exportTplModalVisible = false">取消</Button>
          <Button color="primary" :loading="exportTplLoading" @click="handleExportTpl">导出</Button>
        </template>
      </view-dialog>
    </rs-modal>
    <!-- 字段映射配置弹窗 -->
    <rs-modal ref="fieldMapModal" v-model="fieldMapModalVisible" :width="680">
      <view-dialog title="字段映射配置" class="d-width">
        <template slot="body">
          <div class="field-map-config">
            <div class="field-map-info" v-if="fieldMapSourceName || fieldMapTargetName">
              <span v-if="fieldMapSourceName">源: <b>{{ fieldMapSourceName }}</b></span>
              <span v-if="fieldMapTargetName" style="margin-left:16px;">目标: <b>{{ fieldMapTargetName }}</b></span>
            </div>
            <div v-if="fieldMapSourceFields.length === 0 || fieldMapTargetFields.length === 0" class="field-map-empty">
              请先选择"选入页面"和"选入目标"后再配置字段映射
            </div>
            <template v-else>
              <div class="field-map-header">
                <span class="field-map-col">源字段 (选入数据)</span>
                <span class="field-map-arrow">→</span>
                <span class="field-map-col">目标字段 (子表)</span>
                <span class="field-map-act"></span>
              </div>
              <div v-for="(row, ri) in fieldMapRows" :key="ri" class="field-map-row">
                <Select v-model="row.source" :datas="fieldMapSourceFields" placeholder="选择源字段" style="flex:1;" :filterable="true" />
                <span class="field-map-arrow">=</span>
                <Select v-model="row.target" :datas="fieldMapTargetFields" placeholder="选择目标字段" style="flex:1;" :filterable="true" />
                <span class="field-map-act" @click="removeFieldMapRow(ri)" title="删除"><i class="h-icon-trash"></i></span>
              </div>
              <Button size="s" icon="h-icon-plus" @click="addFieldMapRow" style="margin-top:8px;">添加映射</Button>
            </template>
          </div>
        </template>
        <template slot="footer">
          <Button @click="fieldMapModalVisible = false">取消</Button>
          <Button color="primary" @click="applyFieldMap">确定</Button>
        </template>
      </view-dialog>
    </rs-modal>
  </div>
</template>

<script>
import { mapDateTable, Constants } from '../store';
import pagePreview from './components/page-preview.vue';
import sfcEditorPopup from '@/components/generic-module/sfc-editor-popup.vue';
import codeEditorPopup from '@/components/generic-module/code-editor-popup.vue';
import scriptFlowEditor from '@/components/generic-module/script-flow-editor.vue';
import { invalidateCacheByPrefix } from '@/sfc-loader';
import '@/pages/s01/m01/store';
import uiSetFull from '@/pages/s01/m01/views/uiSetFull.vue';
import moduleWizard from './components/module-wizard.vue';
import configSelPopup from './components/config-sel-popup.vue';
import { PAGE_TPL_DEFAULTS, BTN_PRESETS, BTN_ICON_QUICK, BTN_POPTIP_OPTIONS, BTN_SHOWCOND_OPTIONS, BTN_EXTPARAM_OPTIONS, BTN_FORM_DEFAULTS, SUB_PAGE_FORM_DEFAULTS, ACTION_TYPE_OPTIONS } from '@/constants';

export default {
  name: 's01-m18-config',
  components: { pagePreview, uiSetFull, sfcEditorPopup, codeEditorPopup, scriptFlowEditor, moduleWizard, configSelPopup },
  props: {
    moduleCodeProp: { type: String, default: '' },
    hideToolbar: { type: Boolean, default: false }
  },
  computed: {
    ...mapDateTable('MAIN', []),
    ...mapDateTable('MODPAGE', []),
    ...mapDateTable('MODBUTTON', []),
    // 业务分类下拉：数据字典「业务分类」(D0707)
    bizCatOptions() {
      var d = (this.$store.state.app && this.$store.state.app.dicts['业务分类']) || {};
      return Object.keys(d).map(k => ({ key: k, title: d[k] }));
    },
    moduleCode() {
      if (this.moduleCodeProp) return this.moduleCodeProp;
      return this.$store.state[Constants.STORE_NAME].configModuleCode;
    },
    // 子表路径选项(从 MODPATHREF 取), 用于 subtable 按钮选择归属子表
    subPaths() {
      var appState = this.$store.state.app;
      var modData = appState && appState.modules && appState.modules[this.moduleCode];
      if (!modData || !modData.MODPATHREF) return [];
      var seen = {};
      var paths = [];
      modData.MODPATHREF.forEach(function(ref) {
        if (!seen[ref.PATHNAMEB]) { seen[ref.PATHNAMEB] = true; paths.push({ key: ref.PATHNAMEB, title: ref.PATHNAMEB }) }
      });
      return paths;
    },
    // 按钮区域: 固定 header/footer/row + 各子表路径(DTSA/DTSB...)作为独立区域
    btnAreas() {
      var fixed = ['header', 'footer', 'row'];
      var subs = (this.subPaths || []).map(function(s) { return s.key });
      return fixed.concat(subs);
    },
    // 按钮区域下拉选项
    btnAreaOptions() {
      var fixed = [
        { key: 'header', title: 'header' },
        { key: 'footer', title: 'footer' },
        { key: 'row', title: 'row' }
      ];
      var subs = (this.subPaths || []).map(function(s) {
        return { key: s.key, title: '子表：' + s.key };
      });
      return fixed.concat(subs);
    },
    // 按钮模板列表(用于"一键应用"模板): 直接取自 btnPresets
    btnTemplates() {
      return this.btnPresets;
    },
    // BTNCODE 快捷标签(用于 Select 标签): 从 btnPresets 派生 + 自定义项
    btnCodeOptions() {
      var list = this.btnPresets.map(function(p) {
        return {
          key: p.BTNCODE,
          title: p.BTNNAME,
          btntype: p.BTNTYPE,
          actionType: p.ACTIONTYPE,
          openMode: p.OPENMODE,
          icon: p.ICON,
          color: p.COLOR
        };
      });
      list.push({ key: 'custom', title: '自定义', btntype: 'custom', actionType: 'api', openMode: '', icon: '', color: '' });
      return list;
    },
    modulePaths() {
      var appState = this.$store.state.app;
      if (appState && appState.modules && appState.modules[this.moduleCode]) {
        return appState.modules[this.moduleCode].MODPATH || [];
      }
      return [];
    },
    modPathOptions() {
      return this.modulePaths.map(function(p) {
        return { key: p.PATHNAME, title: p.PATHNAME + (p.RESOURCENAME ? ' (' + p.RESOURCENAME + ')' : '') };
      });
    },
    // select 类型的页面选项（当前模块的 MODPAGE 中 PAGETYPE=select）
    selectPageOptions() {
      var appState = this.$store.state.app;
      if (!appState || !appState.modules) return [];
      var options = [];
      // 遍历所有已加载模块，收集 select 类型的页面
      var self = this;
      Object.keys(appState.modules).forEach(function(modCode) {
        var modData = appState.modules[modCode];
        if (!modData || !modData.MODPAGE) return;
        modData.MODPAGE.forEach(function(p) {
          if (p.PAGETYPE === 'select' && (p.ISDELETED || 0) === 0) {
            var label = p.PAGECODE + (p.PAGENAME ? ' (' + p.PAGENAME + ')' : '');
            if (modCode !== self.moduleCode) label += ' [' + modCode + ']';
            // key 格式: "MODULECODE/PAGECODE"，运行时解析
            options.push({ key: modCode + '/' + p.PAGECODE, title: label });
          }
        });
      });
      return options;
    },
    // 选入目标子表路径选项（从 MODPATHREF 取子表路径，附带 MODPATH 的资源名）
    selectTargetOptions() {
      var appState = this.$store.state.app;
      var modData = appState && appState.modules && appState.modules[this.moduleCode];
      if (!modData || !modData.MODPATHREF) return [];
      var modPaths = modData.MODPATH || [];
      var seen = {};
      var paths = [];
      modData.MODPATHREF.forEach(function(ref) {
        if (seen[ref.PATHNAMEB]) return;
        seen[ref.PATHNAMEB] = true;
        var mpItem = modPaths.find(function(p) { return p.PATHNAME === ref.PATHNAMEB });
        var title = ref.PATHNAMEB;
        if (mpItem && mpItem.RESOURCENAME) {
          title = ref.PATHNAMEB + ' (' + mpItem.RESOURCENAME + ')';
        }
        paths.push({ key: ref.PATHNAMEB, title: title });
      });
      return paths;
    },
    flowCode() {
      var appState = this.$store.state.app;
      var modData = appState && appState.modules && appState.modules[this.moduleCode];
      if (!modData) return '';
      // app.modules 中存的是 A03 原始数据(非 Moudle 实例), MOD 是数组
      var mod = modData.MOD;
      if (Array.isArray(mod) && mod.length > 0) return mod[0].FLOWCODE || '';
      if (mod && mod.FLOWCODE) return mod.FLOWCODE;
      return '';
    },
    moduleApis() {
      return this.$store.state[Constants.STORE_NAME].moduleApis || [];
    },
    moduleName() {
      var dt = this.$MAIN;
      if (dt && dt.data && dt.data.length > 0) {
        return dt.getValue('MODULENAME') || '';
      }
      return '';
    },
    pages() {
      var dt = this.$MODPAGE;
      if (!dt || !dt.data) return [];
      return dt.data.filter(function(p) {
        return (p.ISDELETED || 0) !== 1 && !p.PARENTID;
      }).sort(function(a, b) {
        return (a.SORTNO || 0) - (b.SORTNO || 0);
      });
    },
    currentPage() {
      if (this.selectedIdx === null || this.selectedIdx === undefined) return null;
      var self = this;
      var found = null;
      this.pages.forEach(function(p) {
        if (!found && p._idx_ === self.selectedIdx) found = p;
      });
      return found;
    },
    currentButtons() {
      if (!this.currentPage) return [];
      var pageId = this.currentPage.ID;
      var dt = this.$MODBUTTON;
      if (!dt || !dt.data) return [];
      return dt.data.filter(function(b) {
        return b.PAGEID === pageId && (b.ISDELETED || 0) !== 1;
      }).sort(function(a, b) {
        return (a.SORTNO || 0) - (b.SORTNO || 0);
      });
    },
    pageTypeOptions() {
      return [
        { key: 'list', title: 'list (列表)' },
        { key: 'form', title: 'form (表单)' },
        { key: 'select', title: 'select (选择)' },
        { key: 'review', title: 'review (审核)' },
        { key: 'report', title: 'report (报表)' }
      ];
    },
    compTypeOptions() {
      return [
        { key: 'standard', title: 'standard (标准)' },
        { key: 'sfc', title: 'sfc (在线组件)' }
      ];
    },
    apiOptions() {
      return this.moduleApis.map(function(a) {
        return { key: a.APICODE, title: a.APICODE + (a.APITYPE ? ' (' + a.APITYPE + ')' : '') };
      });
    },
    uiSetFullTitle() {
      return this.uiSetResourceName ?
        '页面配置 - ' + this.uiSetResourceName :
        '页面配置';
    },
    pageConfigJson() {
      if (!this.currentPage || !this.currentPage.PAGECONFIG) return {};
      try {
        return JSON.parse(this.currentPage.PAGECONFIG);
      } catch (e) {
        return {};
      }
    },
    pageTplOptions() {
      var dictItems = this.$store.state.app.dicts['MODPAGE_TPL'] || {};
      var keys = Object.keys(dictItems);
      if (keys.length > 0) {
        return keys.map(function(k) { return { key: k, title: dictItems[k] } });
      }
      // 字典未加载时用默认选项
      return [
        { key: 'list', title: '列表页' },
        { key: 'form', title: '表单页' },
        { key: 'select', title: '选择页' }
      ];
    },
    currentSubPages() {
      return this.pageConfigJson.SUBPAGES || [];
    },
    isCurrentPageList() {
      return this.currentPage && this.currentPage.PAGETYPE === 'list';
    },
    // 当前激活的页面配置（可能是父页面、子页面或嵌套子页面）
    activePageConfig() {
      if (this.selectedSubIdx2 !== null && this.selectedSubIdx !== null && this.currentPage) {
        // 第二层嵌套子页面
        var spList2 = this.getSubPagesOfSub(this.selectedSubIdx);
        var sp2 = spList2[this.selectedSubIdx2];
        if (sp2) return this.resolveSubPageConfig(sp2);
      }
      if (this.selectedSubIdx !== null && this.currentPage) {
        var spList = this.getSubPagesOf(this.currentPage);
        var sp = spList[this.selectedSubIdx];
        if (!sp) return this.currentPage;
        return this.resolveSubPageConfig(sp) || this.currentPage;
      }
      return this.currentPage;
    },
    isSubPageReadonly() {
      if (this.selectedSubIdx2 !== null) {
        var spList2 = this.getSubPagesOfSub(this.selectedSubIdx);
        var sp2 = spList2[this.selectedSubIdx2];
        return sp2 && !!sp2.REFMODULECODE;
      }
      if (this.selectedSubIdx !== null) {
        var spList = this.getSubPagesOf(this.currentPage);
        var sp = spList[this.selectedSubIdx];
        return sp && !!sp.REFMODULECODE;
      }
      return false;
    },
    // 当前页面的子页面选项（供按钮 openForm 目标页面选择）
    formPageOptions() {
      if (!this.currentPage) return [];
      var spList = this.getSubPagesOf(this.currentPage);
      if (spList.length === 0) return [];
      var allPages = (this.$MODPAGE && this.$MODPAGE.data) ? this.$MODPAGE.data : [];
      return spList.map(function(sp, idx) {
        var key, label;
        if (sp.REFMODULECODE) {
          // 引用其他模块的子页面
          key = sp.REFPAGECODE || '';
          label = (sp.PAGENAME || sp.REFPAGECODE || ('子页面' + (idx + 1))) + ' [引用:' + sp.REFMODULECODE + ']';
        } else if (sp.PAGEID) {
          // 自定义子页面：从 MODPAGE 数据中查找 PAGECODE
          var pageRecord = allPages.find(function(p) { return p.ID === sp.PAGEID });
          key = pageRecord ? pageRecord.PAGECODE : sp.PAGEID;
          label = (sp.PAGENAME || (pageRecord && pageRecord.PAGECODE) || ('子页面' + (idx + 1))) + ' [自定义]';
        } else {
          key = '';
          label = '子页面' + (idx + 1);
        }
        return { key: key, title: label };
      });
    },
    // 当前激活页面的按钮列表
    activeButtons() {
      var pc = this.activePageConfig;
      if (!pc) return [];
      var pageId = pc.ID;
      // 引用模式：从目标模块取按钮
      if (this.isSubPageReadonly) {
        var spList = this.getSubPagesOf(this.currentPage);
        var sp = spList[this.selectedSubIdx];
        var refData = this.$store.state.app.modules[sp.REFMODULECODE];
        if (refData && refData.MODBUTTON) {
          return refData.MODBUTTON.filter(function(b) {
            return b.PAGEID === pageId && (b.ISDELETED || 0) !== 1;
          }).sort(function(a, b) { return (a.SORTNO || 0) - (b.SORTNO || 0) });
        }
        return [];
      }
      // 自定义/父页面：从本模块 MODBUTTON 取
      var dt = this.$MODBUTTON;
      if (!dt || !dt.data) return [];
      return dt.data.filter(function(b) {
        return b.PAGEID === pageId && (b.ISDELETED || 0) !== 1;
      }).sort(function(a, b) { return (a.SORTNO || 0) - (b.SORTNO || 0) });
    },
    activePageConfigJson() {
      if (!this.activePageConfig || !this.activePageConfig.PAGECONFIG) return {};
      try {
        return JSON.parse(this.activePageConfig.PAGECONFIG);
      } catch (e) {
        return {};
      }
    },
    // 当前页面可用的 slot 名称列表
    availableSlotNames() {
      if (!this.activePageConfig) return [];
      var pt = this.activePageConfig.PAGETYPE;
      if (pt === 'list' || pt === 'review' || pt === 'select') {
        return ['header-action', 'footer-action', 'table-action', 'simple-query', 'body-query'];
      }
      if (pt === 'form') {
        var names = ['form-top', 'form-bottom'];
        // 从 scm 读取表单字段，生成 field:xxx slot 名
        var json = this.activePageConfigJson;
        var mainPath = json.MAINPATH || 'MAIN';
        var modData = this.$store.state.app.modules[this.activeModuleCode];
        if (modData && modData.MODPATH) {
          var mpItem = modData.MODPATH.find(function(p) { return p.PATHNAME === mainPath });
          if (mpItem && mpItem.RESOURCENAME) {
            var scms = (this.$store.state.app.scms || {})[mpItem.RESOURCENAME] || [];
            scms.forEach(function(f) {
              if (f.EDITSORT && +f.EDITSORT > 0 && f.FIELDNAME) {
                names.push('field:' + f.FIELDNAME);
              }
            });
          }
        }
        return names;
      }
      return [];
    },
    activeModuleCode() {
      // 子页面选中时，引用模式返回目标模块编码
      if (this.selectedSubIdx !== null && this.currentPage) {
        var spList = this.getSubPagesOf(this.currentPage);
        var sp = spList[this.selectedSubIdx];
        if (sp && sp.REFMODULECODE) return sp.REFMODULECODE;
      }
      // 嵌套子页面
      if (this.selectedSubIdx2 !== null && this.selectedSubIdx !== null) {
        var spList2 = this.getSubPagesOfSub(this.selectedSubIdx);
        var sp2 = spList2[this.selectedSubIdx2];
        if (sp2 && sp2.REFMODULECODE) return sp2.REFMODULECODE;
      }
      return this.moduleCode;
    }
  },
  data() {
    // 保存 Vue 实例引用, TreePicker 的 getTotalDatas 回调内用 self 访问组件状态
    // (回调内 this 是 TreePicker 实例, 不是 Vue 组件)
    var self = this;
    return {
      saving: false,
      leftWidth: 360,
      propsHeight: 0,
      isResizing: false,
      wizardVisible: false,
      // 发布到菜单
      publishModalVisible: false,
      publishing: false,
      // 导出为模板弹窗
      exportTplModalVisible: false,
      exportTplLoading: false,
      exportTplForm: { templateCode: '', templateName: '', category: '', description: '' },
      // 模块脚本编辑上下文（extendjs 保存后回填 EXTENDJS 路径用）
      jsEditContext: null,
      publishForm: {
        funcName: '',
        targetId: ''
      },
      publishTreeOption: {
        keyName: 'ID',
        parentName: 'UPFUNCID',
        titleName: 'FUNCNAME',
        dataMode: 'list',
        // 加载所有菜单(含目录和模块), 供发布目标选择
        // RS_M03/A08 只返回目录(F02 过滤器 FUNCTYPE='1'), 这里用 A01 取全部
        getTotalDatas: function(cb) {
          self.$callAction({
            action: Constants.STORE_NAME + '/loadPublishMenus',
            isBusy: false,
          }).then(function(list) {
            cb && cb(list || []);
          }).catch(function() {
            // eslint-disable-next-line standard/no-callback-literal
            cb && cb([]);
          });
        }
      },
      publishMenuList: [],
      isPageFullscreen: false,
      selectedIdx: null,
      selectedSubIdx: null,
      selectedSubIdx2: null,
      btnTypeOptions: [
        { key: 'crud', title: 'crud' }, { key: 'flow', title: 'flow' },
        { key: 'custom', title: 'custom' }, { key: 'batch', title: 'batch' }
      ],
      btnInteractOptions: [
        { key: 'direct', title: 'direct' }, { key: 'poptip', title: 'poptip' }
      ],
      btnColorOptions: [
        { key: '', title: '默认' }, { key: 'primary', title: 'primary' },
        { key: 'red', title: 'red' }, { key: 'green', title: 'green' },
        { key: 'yellow', title: 'yellow' }
      ],
      actionTypeOptions: ACTION_TYPE_OPTIONS,
      // 按钮预设元数据(统一表): 一处定义, btnTemplates/btnCodeOptions 都从此派生
      btnPresets: BTN_PRESETS,
      // 图标快捷标签（常用）
      btnIconQuick: BTN_ICON_QUICK,
      // Poptip 提示下拉
      btnPoptipOptions: BTN_POPTIP_OPTIONS,
      // SHOWCOND 显隐条件快捷标签
      btnShowCondOptions: BTN_SHOWCOND_OPTIONS,
      // EXTPARAM 扩展参数快捷标签
      btnExtparamOptions: BTN_EXTPARAM_OPTIONS,
      btnModalVisible: false,
      btnModalTitle: '新增按钮',
      btnModalMode: 'add',
      btnModalTarget: null,
      btnForm: Object.assign({}, BTN_FORM_DEFAULTS),
      uiSetFullShow: false,
      uiSetResourceId: '',
      uiSetResourceName: '',
      uiSetFullSaving: false,
      uiSetFullType: '',
      selectedTpl: 'list',
      // SFC 编辑器弹窗
      sfcEditorTarget: '', // 'extendjs' | 'sfcmodulepath' | 'sub_sfcmodulepath' | 'slot'
      sfcEditorTitle: 'SFC 编辑器',
      sfcEditorSlotName: '', // 当前编辑的 slot 名称（target='slot' 时有效）
      // 字段映射弹窗
      fieldMapModalVisible: false,
      fieldMapRows: [], // [{ source: '', target: '' }]
      fieldMapSourceFields: [], // [{ key, title }]
      fieldMapTargetFields: [], // [{ key, title }]
      fieldMapSourceName: '',
      fieldMapTargetName: '',
      // 子页面配置
      subPageModalVisible: false,
      subPageModalMode: 'add',
      subPageEditIdx: -1,
      subPageForm: Object.assign({}, SUB_PAGE_FORM_DEFAULTS),
      refModuleOptions: [],
      refPageOptions: []
    };
  },
  async created() {
    console.log('[s01/m18-config] created', this.$route.query.moduleCode);
    var mc = this.moduleCodeProp || this.$route.query.moduleCode;
    if (mc) {
      // 先加载 app 模块配置(含 MODPATHREF 子表关系), 再加载 s01/m18 自身配置
      // 否则 subPaths/btnAreas 首次渲染为空, 子表按钮区域不显示
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initModule', mc);
      await this.$callAction({ action: Constants.STORE_NAME + '/openConfig', param: { MODULECODE: mc }, isBusy: false });
    }
    document.addEventListener('fullscreenchange', this._handleFsChange);
    document.addEventListener('webkitfullscreenchange', this._handleFsChange);
  },
  activated() {
    console.log('[s01/m18-config] activated (keep-alive 缓存命中)');
  },
  watch: {
    moduleCodeProp(val) {
      if (val) {
        this.selectedIdx = null;
        this.$callAction({ action: Constants.STORE_NAME + '/openConfig', param: { MODULECODE: val }, isBusy: false });
      }
    },
    '$route.query.moduleCode': function(val) {
      if (val && val !== this.moduleCode) {
        this.selectedIdx = null;
        this.$callAction({ action: Constants.STORE_NAME + '/openConfig', param: { MODULECODE: val }, isBusy: false });
      }
    },
    // 页面列表加载后，默认选中第一个页面
    pages() {
      if (this.selectedIdx === null && this.pages.length > 0) {
        this.selectedIdx = this.pages[0]._idx_;
      }
    }
  },
  beforeDestroy() {
    document.removeEventListener('fullscreenchange', this._handleFsChange);
    document.removeEventListener('webkitfullscreenchange', this._handleFsChange);
  },
  methods: {
    // ============ 模块创建向导 ============
    openWizard() {
      this.wizardVisible = true;
      this.$refs.wizardModal.show();
    },
    closeWizard() {
      this.wizardVisible = false;
      this.$refs.wizardModal.hide();
    },
    async onWizardDone(payload) {
      this.wizardVisible = false;
      this.$refs.wizardModal.hide();
      // 刷新 app 模块缓存并切换到新模块配置
      if (payload && payload.moduleCode) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', payload.moduleCode);
        await this.$callAction({ action: Constants.STORE_NAME + '/openConfig', param: { MODULECODE: payload.moduleCode }, isBusy: false });
      }
    },
    _handleFsChange() {
      var fsEl = document.fullscreenElement || document.webkitFullscreenElement;
      this.isPageFullscreen = fsEl === this.$refs.configPage;
    },
    togglePageFullscreen() {
      var el = this.$refs.configPage;
      var fsEl = document.fullscreenElement || document.webkitFullscreenElement;
      if (fsEl) {
        if (document.exitFullscreen) document.exitFullscreen();
        else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
      } else {
        if (el.requestFullscreen) el.requestFullscreen();
        else if (el.webkitRequestFullscreen) el.webkitRequestFullscreen();
      }
    },
    goBack() {
      this.$emit('close');
    },
    ptIcon(type) {
      var m = { list: '☰', form: '☐', select: '⊡', review: '✓', report: '▣' };
      return m[type] || '☐';
    },
    selectPage(page) {
      this.selectedIdx = page._idx_;
      this.selectedSubIdx = null;
      this.selectedSubIdx2 = null;
    },
    _genId() {
      return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0;
        var v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
      });
    },
    // btnForm 默认值工厂: 所有字段集中定义, 消除 openAdd/openEdit/applyTemplate 多处重复
    getDefaultBtnForm() {
      return {
        BTNNAME: '',
        BTNCODE: 'custom',
        BTNTYPE: 'custom',
        BTNAREA: 'header',
        INTERACTTYPE: 'direct',
        APICODE: '',
        PERMCODE: '',
        COLOR: '',
        ICON: '',
        POPTIPTEXT: '',
        SHOWCOND: '',
        EXTPARAM: '',
        ACTIONTYPE: 'api',
        OPENMODE: 'add',
        SELECTMODE: 'single',
        SELECTMODULE: '',
        SELECTPAGECODE: '',
        SELECTTARGET: '',
        FIELDMAP: '',
        SELECTWIDTH: 900,
        BEFOREACTION: '',
        AFTERACTION: '',
        EXTRAPARAMS: '',
        FORMPAGECODE: '',
        MODALWIDTH: '',
        MODALFULLSCREEN: false,
        SORTNO: 1
      };
    },
    // 根据流程类型(FLOWCODE)生成默认审批流按钮配置
    // pageType: 'list' | 'form' | 'select'
    // flowCode: '' | '1' | '2'
    getFlowDefaultButtons(pageType, flowCode) {
      if (!flowCode || pageType === 'select') return [];
      var btns = [];
      if (pageType === 'list') {
        // 列表页 footer 审批流按钮（批量操作选中行）
        btns.push({ BTNCODE: 'submit', BTNNAME: '提交', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A17', INTERACTTYPE: 'direct', ICON: 'h-icon-complete', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: '_checks_.every(r=>r.STATE===1)' });
        btns.push({ BTNCODE: 'reSubmit', BTNNAME: '撤销提交', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A18', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: '_checks_.every(r=>r.STATE===2)', POPTIPTEXT: '确定撤销提交？' });
        btns.push({ BTNCODE: 'check', BTNNAME: '审核', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A12', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: '_checks_.every(r=>r.STATE===2)' });
        // 撤销审核: FLOWCODE=1 已审核=3, FLOWCODE=2 待审批=5/19
        var reCheckListCond = flowCode === '2' ? '_checks_.every(r=>r.STATE in [5,19])' : '_checks_.every(r=>r.STATE===3)';
        btns.push({ BTNCODE: 'reCheck', BTNNAME: '撤销审核', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A13', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: reCheckListCond, POPTIPTEXT: '确定撤销审核？' });
        if (flowCode === '2') {
          btns.push({ BTNCODE: 'verify', BTNNAME: '审批', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A14', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: '_checks_.every(r=>r.STATE in [5,19])' });
          btns.push({ BTNCODE: 'reVerify', BTNNAME: '撤销审批', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A15', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: '_checks_.every(r=>r.STATE in [6,20])', POPTIPTEXT: '确定撤销审批？' });
        }
      } else if (pageType === 'form') {
        // 表单页 footer 审批流按钮（单条操作当前单据）
        btns.push({ BTNCODE: 'save', BTNNAME: '暂存', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A04', INTERACTTYPE: 'direct', ICON: 'h-icon-save', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: 'STATE in [1]&&CREATEID==_USERID_' });
        btns.push({ BTNCODE: 'delete', BTNNAME: '删除', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A07', INTERACTTYPE: 'poptip', ICON: 'h-icon-trash', COLOR: 'red', ACTIONTYPE: 'api', SHOWCOND: 'STATE in [1]&&CREATEID==_USERID_&&ID!=null', POPTIPTEXT: '确定删除？' });
        btns.push({ BTNCODE: 'submit', BTNNAME: '提交', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A17', INTERACTTYPE: 'direct', ICON: 'h-icon-complete', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: 'STATE in [1]&&CREATEID==_USERID_' });
        btns.push({ BTNCODE: 'reSubmit', BTNNAME: '撤销提交', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A18', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: 'STATE===2&&CREATEID==_USERID_', POPTIPTEXT: '确定撤销提交？' });
        btns.push({ BTNCODE: 'check', BTNNAME: '审核', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A12', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: 'STATE===2' });
        // 撤销审核: FLOWCODE=1 已审核=3, FLOWCODE=2 待审批=5/19
        var reCheckFormCond = flowCode === '2' ? 'STATE in [5,19]' : 'STATE===3';
        btns.push({ BTNCODE: 'reCheck', BTNNAME: '撤销审核', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A13', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: reCheckFormCond, POPTIPTEXT: '确定撤销审核？' });
        if (flowCode === '2') {
          btns.push({ BTNCODE: 'verify', BTNNAME: '审批', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A14', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api', SHOWCOND: 'STATE in [5,19]' });
          btns.push({ BTNCODE: 'reVerify', BTNNAME: '撤销审批', BTNTYPE: 'flow', BTNAREA: 'footer', APICODE: 'A15', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', ACTIONTYPE: 'api', SHOWCOND: 'STATE in [6,20]', POPTIPTEXT: '确定撤销审批？' });
        }
      }
      return btns;
    },
    // 应用预设(模板或 BTNCODE 快捷标签)到 btnForm, 合并 applyBtnTemplate/applyBtncode 的重复逻辑
    // opts.override=true 全字段覆盖(模板模式); false 则 BTNNAME/ICON/COLOR/PERMCODE 仅填空(标签模式)
    // opts.applyArea=true 同时覆盖 BTNAREA(仅模板模式启用)
    applyPreset(preset, opts) {
      if (!preset) return;
      opts = opts || {};
      var f = this.btnForm;
      // 语义类字段: 始终覆盖(用户期望点预设就生效)
      var alwaysCover = ['BTNCODE', 'BTNTYPE', 'INTERACTTYPE', 'APICODE', 'POPTIPTEXT', 'ACTIONTYPE', 'OPENMODE', 'SELECTMODE'];
      alwaysCover.forEach(function(k) {
        if (preset[k] !== undefined) f[k] = preset[k];
      });
      // 配置值字段: 模板模式覆盖, 标签模式仅填空(保留用户已输入)
      var fillOnlyFields = ['SELECTMODULE', 'SELECTPAGECODE', 'SELECTTARGET', 'FIELDMAP', 'SELECTWIDTH', 'MODALWIDTH', 'FORMPAGECODE'];
      fillOnlyFields.forEach(function(k) {
        if (preset[k] !== undefined && (opts.override || !f[k])) f[k] = preset[k];
      });
      // BTNNAME/ICON/COLOR: 模板模式覆盖, 标签模式仅填空(保留用户已输入)
      ['BTNNAME', 'ICON', 'COLOR'].forEach(function(k) {
        if (preset[k] !== undefined && (opts.override || !f[k])) f[k] = preset[k];
      });
      // BTNAREA: 仅 applyArea=true 时覆盖(避免点 BTNCODE 标签意外改变区域)
      if (opts.applyArea && preset.BTNAREA) f.BTNAREA = preset.BTNAREA;
      // PERMCODE: 仅模板模式自动生成
      if (opts.override && this.moduleCode && preset.APICODE) {
        f.PERMCODE = this.moduleCode + '/' + preset.APICODE;
      }
    },
    addPage() {
      var tpl = PAGE_TPL_DEFAULTS[this.selectedTpl] || null;
      this._addPageWithTpl(tpl);
    },
    _addPageWithTpl(tpl) {
      var dt = this.$MODPAGE;
      if (!dt) return;
      var pageId = this._genId();
      var item = {
        ID: pageId,
        MODULECODE: this.moduleCode,
        PAGETYPE: tpl ? (tpl.PAGETYPE || 'list') : 'list',
        COMPONENTTYPE: tpl ? (tpl.COMPONENTTYPE || 'standard') : 'standard',
        QUERYAPICODE: tpl ? (tpl.QUERYAPICODE || '') : '',
        ADVQUERYAPICODE: tpl ? (tpl.ADVQUERYAPICODE || '') : '',
        OPENAPICODE: tpl ? (tpl.OPENAPICODE || '') : '',
        SAVEAPICODE: tpl ? (tpl.SAVEAPICODE || '') : '',
        PAGECONFIG: tpl ? (tpl.PAGECONFIG || '') : '',
        ISDELETED: 0,
        SORTNO: this.pages.length + 1
      };
      this.$store.commit(Constants.STORE_NAME + '/ADD', {
        path: 'MODPAGE',
        item: item
      });
      if (tpl && tpl.buttons && tpl.buttons.length > 0) {
        var btnDt = this.$MODBUTTON;
        if (btnDt) {
          var self = this;
          tpl.buttons.forEach(function(btn) {
            self.$store.commit(Constants.STORE_NAME + '/ADD', {
              path: 'MODBUTTON',
              item: Object.assign({}, btn, {
                ID: self._genId(),
                PAGEID: pageId,
                MODULECODE: self.moduleCode,
                ISDELETED: 0
              })
            });
          });
        }
      }
      // 根据 FLOWCODE 追加审批流按钮
      var pageType = tpl ? (tpl.PAGETYPE || 'list') : 'list';
      var flowBtns = this.getFlowDefaultButtons(pageType, this.flowCode);
      if (flowBtns.length > 0) {
        var btnDt2 = this.$MODBUTTON;
        if (btnDt2) {
          // 收集已有按钮的 APICODE+BTNAREA，避免重复
          var existingKeys = {};
          btnDt2.data.forEach(function(b) {
            if (b.PAGEID === pageId && (b.ISDELETED || 0) === 0) {
              existingKeys[(b.APICODE || '') + '_' + (b.BTNAREA || '')] = true;
            }
          });
          var self2 = this;
          flowBtns.forEach(function(btn) {
            var key = (btn.APICODE || '') + '_' + (btn.BTNAREA || '');
            if (existingKeys[key]) return; // 跳过重复
            self2.$store.commit(Constants.STORE_NAME + '/ADD', {
              path: 'MODBUTTON',
              item: Object.assign({}, btn, {
                ID: self2._genId(),
                PAGEID: pageId,
                MODULECODE: self2.moduleCode,
                ISDELETED: 0
              })
            });
          });
        }
      }
      this.syncAllToAppModules();
      var newPage = dt.data[dt.data.length - 1];
      if (newPage) {
        this.selectedIdx = newPage._idx_;
      }
      this.refreshPreview();
    },
    removePage(page) {
      var dt = this.$MODPAGE;
      if (!dt) return;
      dt.setValue('ISDELETED', 1, page);
      this.syncToAppModules(page, 'ISDELETED', 1);
      if (this.currentPage && this.currentPage._idx_ === page._idx_) {
        this.selectedIdx = null;
      }
      this.refreshPreview();
    },
    setPageField(field, value) {
      if (!this.currentPage) return;
      var dt = this.$MODPAGE;
      if (dt) dt.setValue(field, value, this.currentPage);
      this.syncToAppModules(this.currentPage, field, value);
      this.refreshPreview();
    },
    // 获取 DataTable 中与 activePageConfig 对应的行引用（确保 setValue 能追踪变更）
    _getDtRow(pc) {
      if (!pc) return null;
      var dt = this.$MODPAGE;
      if (!dt || !dt.data) return null;
      // 父页面：直接从 dt.data 取（pages computed 已返回 dt.data 中的引用）
      if (this.selectedSubIdx === null) return pc;
      // 子页面：resolveSubPageConfig 返回的可能是 app.modules 缓存对象，
      // 必须从 DataTable 中按 ID 找到对应行，否则 dt.setValue 无法追踪变更
      return dt.data.find(function(r) { return r.ID === pc.ID }) || pc;
    },
    // 设置当前激活页面（父页面或子页面）的字段
    setActivePageField(field, value) {
      var pc = this.activePageConfig;
      if (!pc) return;
      if (this.selectedSubIdx !== null && !this.isSubPageReadonly) {
        // 子页面（自定义模式）：设值到 MODPAGE 中对应记录
        var row = this._getDtRow(pc);
        var dt = this.$MODPAGE;
        if (dt && row) dt.setValue(field, value, row);
      } else if (this.selectedSubIdx === null) {
        // 父页面
        var dt2 = this.$MODPAGE;
        if (dt2) dt2.setValue(field, value, pc);
      }
      this.syncToAppModules(pc, field, value);
      this.refreshPreview();
    },
    setActivePageConfigField(key, value) {
      var pc = this.activePageConfig;
      if (!pc) return;
      if (this.selectedSubIdx !== null && this.isSubPageReadonly) return;
      var json = this.activePageConfigJson;
      if (value) {
        json[key] = value;
      } else {
        delete json[key];
      }
      var row = this._getDtRow(pc);
      var dt = this.$MODPAGE;
      if (dt && row) dt.setValue('PAGECONFIG', JSON.stringify(json), row);
      this.syncToAppModules(pc, 'PAGECONFIG', JSON.stringify(json));
      this.refreshPreview();
    },
    // slot 名称转显示标签
    slotLabel(slotName) {
      if (slotName.indexOf('field:') === 0) {
        return '字段 ' + slotName.substring(6);
      }
      return slotName;
    },
    // 读取 slot 的 SFC 路径
    getSlotPath(slotName) {
      var json = this.activePageConfigJson;
      return (json.SLOTS && json.SLOTS[slotName]) || '';
    },
    // 设置/清除 slot 的 SFC 路径
    setSlotPath(slotName, path) {
      var pc = this.activePageConfig;
      if (!pc) return;
      if (this.selectedSubIdx !== null && this.isSubPageReadonly) return;
      var json = this.activePageConfigJson;
      if (!json.SLOTS) json.SLOTS = {};
      if (path) {
        json.SLOTS[slotName] = path;
      } else {
        delete json.SLOTS[slotName];
      }
      if (Object.keys(json.SLOTS).length === 0) delete json.SLOTS;
      var row = this._getDtRow(pc);
      var dt = this.$MODPAGE;
      if (dt && row) dt.setValue('PAGECONFIG', JSON.stringify(json), row);
      this.syncToAppModules(pc, 'PAGECONFIG', JSON.stringify(json));
      this.refreshPreview();
    },
    setPageConfigField(key, value) {
      if (!this.currentPage) return;
      var json = this.pageConfigJson;
      if (value) {
        json[key] = value;
      } else {
        delete json[key];
      }
      var dt = this.$MODPAGE;
      if (dt) dt.setValue('PAGECONFIG', JSON.stringify(json), this.currentPage);
      this.syncToAppModules(this.currentPage, 'PAGECONFIG', JSON.stringify(json));
      this.refreshPreview();
    },
    removeBtn(btn) {
      var dt = this.$MODBUTTON;
      if (dt) dt.setValue('ISDELETED', 1, btn);
      this.syncBtnToAppModules(btn, 'ISDELETED', 1);
      this.refreshPreview();
    },
    // 在所属区域内上移(delta=-1)/下移(delta=1): 交换相邻按钮在 dt.data 中的位置 + 同步 SORTNO
    moveBtnInArea(btn, delta) {
      var list = this.activeButtonsByArea(btn.BTNAREA);
      var idx = list.indexOf(btn);
      if (idx < 0) return;
      var newIdx = idx + delta;
      if (newIdx < 0 || newIdx >= list.length) return;
      var swapTarget = list[newIdx];
      var dt = this.$MODBUTTON;
      if (!dt || !dt.data) return;
      var i1 = dt.data.indexOf(btn);
      var i2 = dt.data.indexOf(swapTarget);
      if (i1 < 0 || i2 < 0) return;
      // 记录原始 SORTNO
      var s1 = btn.SORTNO || 0;
      var s2 = swapTarget.SORTNO || 0;
      // 1) 交换 SORTNO: 走 dt.setValue 正式 API, 内部 update() 会自动维护 _modifyIdxRows
      //    (old 类型按钮有 _rawIdxData 快照, 会被标记 modify; new 类型按钮无需 modify, update 自身已处理)
      dt.setValue('SORTNO', s2, btn);
      dt.setValue('SORTNO', s1, swapTarget);
      // 2) 在 dt.data 数组层面交换位置(splice 是 Vue 可监听的数组 mutation)
      if (i1 < i2) {
        dt.data.splice(i2, 1);
        dt.data.splice(i1, 0, swapTarget);
      } else {
        dt.data.splice(i1, 1);
        dt.data.splice(i2, 0, btn);
      }
      // 3) 同步到 app.modules 缓存 + 刷新预览
      this.syncBtnToAppModules(btn, 'SORTNO', s2);
      this.syncBtnToAppModules(swapTarget, 'SORTNO', s1);
      this.refreshPreview();
    },
    setBtnField(btn, field, value) {
      var dt = this.$MODBUTTON;
      if (dt) dt.setValue(field, value, btn);
      this.syncBtnToAppModules(btn, field, value);
      this.refreshPreview();
    },
    buttonsByArea(area) {
      return this.currentButtons.filter(function(b) {
        return b.BTNAREA === area;
      });
    },
    activeButtonsByArea(area) {
      return this.activeButtons.filter(function(b) {
        return b.BTNAREA === area;
      });
    },
    // 判断按钮区域是否为子表(即不在 header/footer/row 中的区域)
    isSubArea(area) {
      return area && area !== 'header' && area !== 'footer' && area !== 'row';
    },
    // 选择按钮模板，自动填充所有字段(覆盖模式 + 套用区域)
    applyBtnTemplate(tpl) {
      // 子表行级模板(subAdd/subRemove/subUp/subDown): BTNAREA 必须是子表路径,
      // 若当前 BTNAREA 不是子表(如默认 header), 自动切到第一个可用子表
      var subCodes = ['subAdd', 'subRemove', 'subUp', 'subDown'];
      if (subCodes.indexOf(tpl.BTNCODE) >= 0) {
        if (!this.isSubArea(this.btnForm.BTNAREA)) {
          var firstSub = this.subPaths[0];
          if (firstSub) this.btnForm.BTNAREA = firstSub.key;
        }
      }
      this.applyPreset(tpl, { override: true, applyArea: true });
    },
    // APICODE 变化时自动补全权限编码
    onApicodeChange(val) {
      if (val && this.moduleCode && !this.btnForm.PERMCODE) {
        this.btnForm.PERMCODE = this.moduleCode + '/' + val;
      }
    },
    // 点击按钮编码快捷标签(唯一入口, 不再监听输入框 @input 避免误触联动)
    selectBtncode(val) {
      this.btnForm.BTNCODE = val;
      this.applyBtncode(val);
    },
    // 根据 BTNCODE 自动填充 ACTIONTYPE/BTNTYPE/ICON/COLOR（匹配预设时, 仅填空避免覆盖用户已输入）
    applyBtncode(val) {
      var preset = this.btnPresets.find(function(p) { return p.BTNCODE === val });
      if (preset) {
        this.applyPreset(preset, { override: false, applyArea: false });
      } else {
        // 自定义编码：默认 api 动作
        this.btnForm.ACTIONTYPE = 'api';
      }
    },
    // 根据模块 FLOWCODE 一键补充当前页面缺失的审批流按钮
    applyFlowButtons() {
      var pc = this.activePageConfig;
      if (!pc) return;
      var pageType = pc.PAGETYPE;
      var flowBtns = this.getFlowDefaultButtons(pageType, this.flowCode);
      if (!flowBtns.length) {
        this.$Message('当前流程无需额外按钮');
        return;
      }
      var pageId = pc.ID;
      var btnDt = this.$MODBUTTON;
      if (!btnDt) return;
      // 收集已有按钮的 APICODE+BTNAREA，避免重复
      var existingKeys = {};
      btnDt.data.forEach(function(b) {
        if (b.PAGEID === pageId && (b.ISDELETED || 0) === 0) {
          existingKeys[(b.APICODE || '') + '_' + (b.BTNAREA || '')] = true;
        }
      });
      var added = 0;
      var sortBase = this._nextBtnSortNo();
      var self = this;
      flowBtns.forEach(function(btn, idx) {
        var key = (btn.APICODE || '') + '_' + (btn.BTNAREA || '');
        if (existingKeys[key]) return;
        self.$store.commit(Constants.STORE_NAME + '/ADD', {
          path: 'MODBUTTON',
          item: Object.assign({}, btn, {
            ID: self._genId(),
            PAGEID: pageId,
            MODULECODE: self.moduleCode,
            ISDELETED: 0,
            SORTNO: sortBase + idx
          })
        });
        added++;
      });
      if (added > 0) {
        this.syncAllToAppModules();
        this.refreshPreview();
        this.$Message('已补充 ' + added + ' 个审批流按钮');
      } else {
        this.$Message('审批流按钮已齐全，无需补充');
      }
    },
    // ============ 从其他模块复制按钮 ============
    openCopyBtnPopup() {
      this.$refs.configSelPopup.openButton();
    },
    onConfigSelConfirm({ mode, data }) {
      if (mode === 'button') {
        this.copyButtonsFromModule(data);
      }
    },
    copyButtonsFromModule(srcBtns) {
      if (!srcBtns || !srcBtns.length || !this.currentPage) return;
      var dt = this.$MODBUTTON;
      if (!dt) return;
      var pageId = this.activePageConfig ? this.activePageConfig.ID : '';
      var self = this;
      srcBtns.forEach(function(srcBtn) {
        var newBtn = {
          ID: '',
          PAGEID: pageId,
          MODULECODE: self.moduleCode,
          BTNNAME: srcBtn.BTNNAME || '',
          BTNCODE: srcBtn.BTNCODE || '',
          BTNTYPE: srcBtn.BTNTYPE || 'custom',
          BTNAREA: srcBtn.BTNAREA || 'header',
          INTERACTTYPE: srcBtn.INTERACTTYPE || 'direct',
          APICODE: srcBtn.APICODE || '',
          PERMCODE: srcBtn.PERMCODE || '',
          COLOR: srcBtn.COLOR || '',
          ICON: srcBtn.ICON || '',
          POPTIPTEXT: srcBtn.POPTIPTEXT || '',
          SHOWCOND: srcBtn.SHOWCOND || '',
          EXTPARAM: srcBtn.EXTPARAM || '',
          ACTIONCODE: srcBtn.ACTIONCODE || '',
          SORTNO: self._nextBtnSortNo(),
          ISDELETED: 0,
        };
        dt.add(newBtn);
      });
      this.syncPageButtonsToApp();
      this.$Message.success('已复制 ' + srcBtns.length + ' 个按钮');
    },
    openAddBtnModal(preset) {
      if (!this.currentPage) return;
      preset = preset || {};
      this.btnModalMode = 'add';
      this.btnModalTitle = '新增按钮';
      this.btnModalTarget = null;
      this.btnForm = this.getDefaultBtnForm();
      if (preset.BTNAREA) this.btnForm.BTNAREA = preset.BTNAREA;
      // 新按钮 SORTNO = 当前 page 内按钮(含未删除)的最大 SORTNO + 1, 保证排到末尾
      this.btnForm.SORTNO = this._nextBtnSortNo();
      this.$refs.btnModal.show();
    },
    // 取当前激活页面(可能是子页面)内按钮的最大 SORTNO + 1, 用于新增按钮时自动递增
    // 注意: 必须用 activePageConfig.ID 而非 currentPage.ID, 与 confirmBtnModal 保存按钮时的 PAGEID 保持一致
    _nextBtnSortNo() {
      var pc = this.activePageConfig;
      var pageId = pc ? pc.ID : '';
      var dt = this.$MODBUTTON;
      var maxSort = 0;
      if (dt && dt.data) {
        dt.data.forEach(function(b) {
          if (b.PAGEID === pageId && (b.ISDELETED || 0) !== 1) {
            var sn = b.SORTNO || 0;
            if (sn > maxSort) maxSort = sn;
          }
        });
      }
      return maxSort + 1;
    },
    openEditBtnModal(btn) {
      this.btnModalMode = 'edit';
      this.btnModalTitle = '编辑按钮';
      this.btnModalTarget = btn;
      this.btnForm = this.getDefaultBtnForm();
      // 回填按钮主字段
      var mainFields = ['BTNNAME', 'BTNCODE', 'BTNTYPE', 'BTNAREA', 'INTERACTTYPE', 'APICODE', 'PERMCODE', 'COLOR', 'ICON', 'POPTIPTEXT', 'SHOWCOND', 'EXTPARAM'];
      var f = this.btnForm;
      mainFields.forEach(function(k) {
        if (btn[k] !== undefined && btn[k] !== null) f[k] = btn[k];
      });
      f.SORTNO = btn.SORTNO || 0;
      // 从 EXTPARAM 反序列化动作配置
      this.parseExtparamToForm(btn.EXTPARAM);
      // 兼容老数据: BTNAREA='subtable' + EXTPARAM.subtable 迁移为 BTNAREA=子表路径
      if (f.BTNAREA === 'subtable') {
        var extLegacy = {};
        try { extLegacy = JSON.parse(btn.EXTPARAM || '{}') } catch (e) {}
        if (extLegacy.subtable) f.BTNAREA = extLegacy.subtable;
      }
      this.$refs.btnModal.show();
    },
    confirmBtnModal() {
      // 序列化动作配置到 EXTPARAM
      var extStr = JSON.stringify(this.buildExtparam());
      var f = this.btnForm;
      // 要保存的字段对象(add/edit 共用, 消除两条重复的字段列表)
      var fields = {
        BTNNAME: f.BTNNAME,
        BTNCODE: f.BTNCODE || 'custom',
        BTNTYPE: f.BTNTYPE,
        BTNAREA: f.BTNAREA,
        INTERACTTYPE: f.INTERACTTYPE,
        APICODE: f.APICODE,
        PERMCODE: f.PERMCODE,
        COLOR: f.COLOR,
        ICON: f.ICON,
        POPTIPTEXT: f.POPTIPTEXT,
        SHOWCOND: f.SHOWCOND,
        EXTPARAM: extStr,
        SORTNO: f.SORTNO || 1
      };
      if (this.btnModalMode === 'add') {
        var targetPageId = this.activePageConfig ? this.activePageConfig.ID : this.currentPage.ID;
        this.$store.commit(Constants.STORE_NAME + '/ADD', {
          path: 'MODBUTTON',
          item: Object.assign({
            ID: this._genId(),
            PAGEID: targetPageId,
            MODULECODE: this.moduleCode,
            ISDELETED: 0
          }, fields)
        });
        this.syncAllToAppModules();
        this.refreshPreview();
      } else if (this.btnModalMode === 'edit' && this.btnModalTarget) {
        var btn = this.btnModalTarget;
        var self = this;
        Object.keys(fields).forEach(function(k) {
          self.setBtnField(btn, k, fields[k]);
        });
      }
      this.btnModalVisible = false;
    },
    // 把 btnForm 的动作配置字段序列化为 EXTPARAM JSON 对象
    buildExtparam() {
      var ext = {};
      var f = this.btnForm;
      // 始终保存 action，确保用户选择的动作类型可还原
      if (f.ACTIONTYPE) ext.action = f.ACTIONTYPE;
      if (f.ACTIONTYPE === 'openForm') {
        ext.openMode = f.OPENMODE || 'add';
        if (f.FORMPAGECODE) ext.formPageCode = f.FORMPAGECODE;
        if (f.MODALWIDTH) ext.modalWidth = f.MODALWIDTH;
        if (f.MODALFULLSCREEN) ext.modalFullScreen = true;
      }
      if (f.ACTIONTYPE === 'openSelector') {
        ext.selectMode = f.SELECTMODE || 'single';
        // SELECTPAGECODE 格式 "MODULECODE/PAGECODE"，解析为 selectModule + selectPageCode
        if (f.SELECTPAGECODE) {
          var parts = f.SELECTPAGECODE.split('/');
          if (parts.length === 2) {
            ext.selectModule = parts[0];
            ext.selectPageCode = parts[1];
          } else {
            ext.selectModule = this.moduleCode;
            ext.selectPageCode = f.SELECTPAGECODE;
          }
        }
        if (f.SELECTTARGET) ext.selectTarget = f.SELECTTARGET;
        if (f.FIELDMAP) ext.fieldMap = f.FIELDMAP;
        if (f.SELECTWIDTH) ext.selectWidth = f.SELECTWIDTH;
      }
      if (f.BEFOREACTION) ext.beforeAction = f.BEFOREACTION;
      if (f.AFTERACTION) ext.afterAction = f.AFTERACTION;
      if (f.EXTRAPARAMS) {
        // 合法 JSON 对象 → 静态 extraParams；否则视为动态参数方法名 → paramsFn
        var parsed = null;
        try { parsed = JSON.parse(f.EXTRAPARAMS) } catch (e) { parsed = null }
        if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
          ext.extraParams = parsed;
        } else {
          ext.paramsFn = f.EXTRAPARAMS;
        }
      }
      return ext;
    },
    // 从 btn.EXTPARAM 反序列化到 btnForm 的动作配置字段
    parseExtparamToForm(extStr) {
      var ext = {};
      if (extStr) {
        try { ext = JSON.parse(extStr) } catch (e) { ext = {} }
      }
      var f = this.btnForm;
      f.ACTIONTYPE = ext.action || 'api';
      f.OPENMODE = ext.openMode || 'add';
      f.FORMPAGECODE = ext.formPageCode || '';
      f.MODALWIDTH = ext.modalWidth || '';
      f.MODALFULLSCREEN = !!ext.modalFullScreen;
      f.SELECTMODE = ext.selectMode || 'single';
      f.SELECTMODULE = ext.selectModule || '';
      // SELECTPAGECODE 合并格式 "MODULECODE/PAGECODE"
      if (ext.selectModule && ext.selectPageCode) {
        f.SELECTPAGECODE = ext.selectModule + '/' + ext.selectPageCode;
      } else {
        f.SELECTPAGECODE = ext.selectPageCode || '';
      }
      f.SELECTTARGET = ext.selectTarget || '';
      f.FIELDMAP = ext.fieldMap || '';
      f.SELECTWIDTH = ext.selectWidth || 900;
      f.BEFOREACTION = ext.beforeAction || '';
      f.AFTERACTION = ext.afterAction || '';
      // 优先 paramsFn（方法名原样字符串），其次 extraParams（JSON 序列化）
      f.EXTRAPARAMS = ext.paramsFn || (ext.extraParams ? JSON.stringify(ext.extraParams) : '');
    },
    // 左右分隔条拖动
    onLeftResizeStart(e) {
      e.preventDefault();
      this.isResizing = true;
      var startX = e.clientX;
      var startWidth = this.leftWidth;
      var mask = document.createElement('div');
      mask.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;cursor:col-resize;';
      document.body.appendChild(mask);
      var self = this;
      var onMove = function(ev) {
        var delta = ev.clientX - startX;
        self.leftWidth = Math.max(280, Math.min(startWidth + delta, window.innerWidth * 0.5));
      };
      var onUp = function() {
        self.isResizing = false;
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
        document.body.removeChild(mask);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    // 上下分隔条拖动
    onPropsResizeStart(e) {
      e.preventDefault();
      this.isResizing = true;
      var startY = e.clientY;
      var el = this.$el.querySelector('.mc-bottom .mc-half');
      var startHeight = this.propsHeight || (el ? el.offsetHeight : 200);
      var mask = document.createElement('div');
      mask.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;cursor:row-resize;';
      document.body.appendChild(mask);
      var self = this;
      var onMove = function(ev) {
        var delta = ev.clientY - startY;
        self.propsHeight = Math.max(150, Math.min(startHeight + delta, window.innerHeight * 0.5));
      };
      var onUp = function() {
        self.isResizing = false;
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
        document.body.removeChild(mask);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    async handleSave() {
      this.saving = true;
      try {
        await this.$callAction({ action: Constants.STORE_NAME + '/saveConfig', isBusy: false });
        this.$alert('保存成功');
        this.$emit('saved');
      } catch (e) {
        // $callAction 失败时已弹错误提示，这里只发事件
        this.$emit('save-error');
      } finally {
        this.saving = false;
      }
    },
    // ====== 发布到菜单 ======
    openPublishModal() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      // 默认菜单名取当前模块的 MODULENAME
      this.publishForm.funcName = this.moduleName || this.moduleCode;
      this.publishForm.targetId = '';
      this.publishModalVisible = true;
    },
    // 跳转版本中心（按模块编码过滤页面/按钮/模块等全部对象类型）
    openVersions() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      this.$router.push({ name: 's01/m22', query: { objCode: this.moduleCode } });
    },
    // ====== 接口脚本（直接打开模块脚本编辑器，左侧列表限定当前模块相关资产） ======
    openCodeFiles() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      this.$refs.codeEditorPopup.show(this.moduleCode);
    },
    openScriptFlowEditor() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      this.$refs.scriptFlowEditor.show(this.moduleCode);
    },
    // Store 扩展：模块脚本编辑器打开 @/modules/{MC}/store.js（不存在则新建）
    openModuleStore() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      this.$refs.codeEditorPopup.openJs('@/modules/' + this.moduleCode + '/store.js', this.moduleCode);
    },
    // 扩展 JS：模块脚本编辑器打开页面扩展文件（未配置时按约定路径）
    openExtendJs() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      var path = this.activePageConfigJson.EXTENDJS || '';
      if (!path && this.activePageConfig) {
        path = '@/modules/' + this.moduleCode + '/' + (this.activePageConfig.PAGECODE || 'form') + '.js';
      }
      this.jsEditContext = 'extendjs';
      this.$refs.codeEditorPopup.openJs(path, this.moduleCode);
    },
    // 模块脚本保存完成：扩展 JS 场景下回填 EXTENDJS 路径到页面配置（原本为空时）
    onCodeAssetSaved({ kind, code, path }) {
      if (kind === 'js' && this.jsEditContext === 'extendjs' && !this.activePageConfigJson.EXTENDJS) {
        // 扩展 JS 以路径为身份：回填 MODULEPATH(@/modules/{模块}/{页面}.js)
        this.setActivePageConfigField('EXTENDJS', path || code);
        this.$Message.success('已回填页面扩展 JS 路径');
      } else if (kind === 'js' && this.jsEditContext === 'btn_hook') {
        // 按钮钩子/显隐/动态参数：方法名已在 openHookEditor 回填 btnForm，保存后刷新预览
        this.refreshPreview();
      }
      // JS 资产保存后刷新预览，使扩展JS变更立即生效
      if (kind === 'js') {
        this.refreshPreview();
      }
      this.jsEditContext = null;
    },
    onScriptFlowSaved() {
      // 编排接口保存后刷新模块配置，使新接口出现在下拉选项
      // eslint-disable-next-line no-restricted-syntax
      this.$store.dispatch('app/initModule', this.moduleCode);
    },
    // ====== 导出为业务模板 ======
    openExportTpl() {
      if (!this.moduleCode) {
        this.$error('请先选择/打开一个模块');
        return;
      }
      this.exportTplForm.templateCode = 'TPL_' + this.moduleCode;
      this.exportTplForm.templateName = (this.moduleName || this.moduleCode) + '模板';
      this.exportTplForm.category = '';
      this.exportTplForm.description = '';
      this.exportTplModalVisible = true;
    },
    async handleExportTpl() {
      if (!this.exportTplForm.templateCode || !this.exportTplForm.templateName) {
        this.$error('模板编码和名称必填');
        return;
      }
      this.exportTplLoading = true;
      try {
        let ret = await this.$callAction({
          action: Constants.STORE_NAME + '/exportTemplate',
          param: {
            moduleCode: this.moduleCode,
            templateCode: this.exportTplForm.templateCode,
            templateName: this.exportTplForm.templateName,
            category: this.exportTplForm.category,
            description: this.exportTplForm.description
          },
          isBusy: false,
        });
        this.$Message.success((ret && ret.message) || '导出成功');
        this.exportTplModalVisible = false;
      } catch (e) {
        this.$error('导出失败: ' + (e.message || e));
      } finally {
        this.exportTplLoading = false;
      }
    },
    // 根据选中的目标(目录/模块)决定发布模式
    resolvePublishMode() {
      var targetId = this.publishForm.targetId;
      if (!targetId) return null;
      var list = (this.$store.state[Constants.STORE_NAME] && this.$store.state[Constants.STORE_NAME].publishMenuList) || [];
      var target = list.find(function(m) { return String(m.ID) === String(targetId); });
      if (!target) return null;
      // FUNCTYPE=1 目录 → 在该目录下新增; FUNCTYPE=2 模块 → 替换
      if (+target.FUNCTYPE === 2) {
        return { mode: 'replace', target: target };
      }
      return { mode: 'new', upFuncId: targetId };
    },
    // 收集当前模块所有按钮(去重)作为功能点来源
    collectPublishButtons() {
      var dt = this.$MODBUTTON;
      if (!dt || !dt.data) return [];
      var seen = {};
      var list = [];
      dt.data.forEach(function(btn) {
        if (btn.ISDELETED === 1) return;
        var apicode = btn.APICODE;
        if (!apicode || seen[apicode]) return;
        seen[apicode] = 1;
        list.push({ APICODE: apicode, BTNNAME: btn.BTNNAME || apicode });
      });
      return list;
    },
    async handlePublish() {
      var resolved = this.resolvePublishMode();
      if (!resolved) {
        this.$error('请选择发布目标(目录或模块)');
        return;
      }
      if (!this.publishForm.funcName || !this.publishForm.funcName.trim()) {
        this.$error('请填写菜单名称');
        return;
      }
      var buttons = this.collectPublishButtons();
      this.publishing = true;
      try {
        await this.$callAction({
          action: Constants.STORE_NAME + '/publish',
          param: {
            mode: resolved.mode,
            targetFuncId: resolved.target ? resolved.target.ID : '',
            upFuncId: resolved.upFuncId || '',
            funcName: this.publishForm.funcName.trim(),
            moduleCode: this.moduleCode,
            buttons: buttons
          },
          isBusy: false,
        });
        this.$alert(resolved.mode === 'new' ? '已发布为新菜单' : '已替换模块信息');
        this.publishModalVisible = false;
      } catch (e) {
        // $callAction 失败时已弹错误提示
      } finally {
        this.publishing = false;
      }
    },
    openUiSet({ type, resourceId, resourceName }) {
      this.uiSetResourceId = resourceId;
      this.uiSetResourceName = resourceName;
      this.uiSetFullType = type;
      this.uiSetFullSaving = false;
      this.uiSetFullShow = true;
      // 打开后设置 activeTab
      this.$nextTick(function() {
        if (this.$refs.uiSetFull) {
          this.$refs.uiSetFull.activeTab = type;
        }
      });
    },
    onUiSetSave() {
      if (this.$refs.uiSetFull) {
        this.$refs.uiSetFull.onSave();
      }
    },
    onUiSetSaved() {
      this.uiSetFullShow = false;
      // 刷新预览
      if (this.$refs.pagePreview) {
        this.$refs.pagePreview.refresh();
      }
    },
    // SFC 编辑器弹窗
    openSfcEditor(target, slotName) {
      this.sfcEditorTarget = target;
      var path = '';
      var fType = 'JS';
      var context = { editTarget: target, moduleCode: this.activeModuleCode };
      if (target === 'extendjs') {
        // 页面扩展 JS
        path = this.activePageConfigJson.EXTENDJS || '';
        if (!path && this.activePageConfig) {
          // 约定路径: @/modules/{moduleCode}/{pageCode}.js
          path = '@/modules/' + this.activeModuleCode + '/' + (this.activePageConfig.PAGECODE || 'form') + '.js';
        }
        this.sfcEditorTitle = '编辑扩展 JS';
        fType = 'JS';
        context.pageCode = this.activePageConfig ? (this.activePageConfig.PAGECODE || '') : '';
      } else if (target === 'sfcmodulepath') {
        // 页面 SFC 组件路径：未配置时按约定 @/modules/{moduleCode}/{pageCode}.vue 自动生成
        path = this.activePageConfig.SFCMODULEPATH || '';
        if (!path && this.activePageConfig) {
          path = '@/modules/' + this.activeModuleCode + '/' + (this.activePageConfig.PAGECODE || 'page') + '.vue';
        }
        this.sfcEditorTitle = '编辑 SFC 组件';
        fType = 'VUE';
        context.editTarget = 'sfc';
        context.pageCode = this.activePageConfig ? (this.activePageConfig.PAGECODE || '') : '';
        // 传主组件路径，文件列表里标星
        context.mainModulePath = this.activePageConfig.SFCMODULEPATH || path;
      } else if (target === 'sub_sfcmodulepath') {
        // 子页面 SFC 组件路径：未配置时按约定 @/modules/{moduleCode}/{pageCode}.vue 自动生成
        path = this.subPageForm.SFCMODULEPATH || '';
        if (!path) {
          var subPageCode = this.subPageForm.PAGECODE || ('sub_' + (this.selectedSubIdx == null ? '' : this.selectedSubIdx));
          path = '@/modules/' + this.activeModuleCode + '/' + subPageCode + '.vue';
        }
        this.sfcEditorTitle = '编辑 SFC 组件';
        fType = 'VUE';
        context.editTarget = 'sfc';
      } else if (target === 'modulestore') {
        // 模块级 store 扩展
        path = '@/modules/' + this.moduleCode + '/store.js';
        this.sfcEditorTitle = '编辑模块 Store 扩展';
        fType = 'JS';
        context.editTarget = 'store';
        context.moduleCode = this.moduleCode;
      } else if (target === 'slot') {
        // SFC slot 扩展
        this.sfcEditorSlotName = slotName || '';
        var existingPath = this.getSlotPath(slotName);
        if (existingPath) {
          path = existingPath;
        } else {
          // 约定路径: @/modules/{moduleCode}/{pageCode}_{slotName_sanitized}.vue
          var sanitized = slotName.replace(/[:/]/g, '_');
          path = '@/modules/' + this.activeModuleCode + '/' +
                (this.activePageConfig.PAGECODE || 'page') + '_' + sanitized + '.vue';
        }
        this.sfcEditorTitle = '编辑 Slot: ' + this.slotLabel(slotName);
        fType = 'VUE';
        context.editTarget = 'sfc';
        context.pageCode = this.activePageConfig ? (this.activePageConfig.PAGECODE || '') : '';
        context.slotName = slotName; // 传递 slot 名称供编辑器生成对应模板
      }
      if (this.$refs.sfcEditor) {
        this.$refs.sfcEditor.show(path, fType, context);
      }
    },
    // 根据按钮编码生成钩子方法名
    genHookMethodName(field) {
      var raw = (this.btnForm.BTNCODE || 'custom').replace(/[^A-Za-z0-9]/g, '');
      var pascal = raw.charAt(0).toUpperCase() + raw.slice(1);
      var upper = raw.toUpperCase();
      if (field === 'beforeAction') return 'before' + pascal;
      if (field === 'afterAction') return 'after' + pascal;
      if (field === 'showCond') return 'ISSHOW' + upper;
      if (field === 'paramsFn') return 'get' + pascal + 'Params';
      return raw;
    },
    // 生成方法骨架（无前导缩进，由编辑器按目标块缩进补齐）
    buildHookSnippet(field, name) {
      if (field === 'beforeAction') {
        return [
          '// 前置钩子：点击按钮前调用，返回 false 中止动作；context={row,ext,btn}',
          name + '(btn, context) {',
          '  // TODO: 实现前置逻辑（可读 this.STATE / context.row）',
          '  // return false;',
          '},'
        ].join('\n');
      }
      if (field === 'afterAction') {
        return [
          '// 后置钩子：动作完成后调用；context={row,ext,btn,result,rows}',
          name + '(btn, context) {',
          '  // TODO: 实现后置逻辑（如刷新列表）',
          '},'
        ].join('\n');
      }
      if (field === 'showCond') {
        return [
          '// 显隐条件：返回 true 显示按钮，false 隐藏（可读 this.STATE / this.ID / this.selectedRows）',
          name + '() {',
          '  // TODO: 实现显隐逻辑',
          '  return true;',
          '},'
        ].join('\n');
      }
      if (field === 'paramsFn') {
        return [
          '// 动态参数：返回参数对象，合并到 API 请求参数；context={row,ext,btn}',
          name + '(btn, context) {',
          '  // TODO: 根据当前行/状态返回动态参数',
          '  return {',
          '    // STATE: context.row.STATE,',
          '  };',
          '},'
        ].join('\n');
      }
      return '';
    },
    // ====== 字段映射弹窗 ======
    async openFieldMapModal() {
      // 解析当前选入页面和选入目标，加载字段列表
      var pageCode = this.btnForm.SELECTPAGECODE || '';
      var targetPath = this.btnForm.SELECTTARGET || '';
      // 解析 selectModule / selectPageCode
      var selectModule = this.moduleCode;
      var selectPageCode = pageCode;
      if (pageCode && pageCode.indexOf('/') > 0) {
        var parts = pageCode.split('/');
        selectModule = parts[0];
        selectPageCode = parts[1];
      }
      // 加载源字段（select 页面的 QRY 资源）
      this.fieldMapSourceFields = [];
      this.fieldMapSourceName = '';
      this.fieldMapTargetFields = [];
      this.fieldMapTargetName = '';
      // 确保模块已加载
      if (selectModule && selectModule !== this.moduleCode) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', selectModule);
      }
      var selModData = this.$store.state.app.modules[selectModule];
      if (selModData && selModData.MODPAGE) {
        var selPage = selModData.MODPAGE.find(function(p) {
          return p.PAGECODE === selectPageCode && (p.ISDELETED || 0) === 0;
        });
        if (selPage) {
          var pageConfig = {};
          try { pageConfig = JSON.parse(selPage.PAGECONFIG || '{}') } catch (e) { pageConfig = {} }
          var qryPathName = pageConfig.QRYPATH || 'QRY';
          if (selModData.MODPATH) {
            var mpItem = selModData.MODPATH.find(function(p) { return p.PATHNAME === qryPathName });
            if (mpItem && mpItem.RESOURCENAME) {
              this.fieldMapSourceName = mpItem.RESOURCENAME;
              // eslint-disable-next-line no-restricted-syntax
              await this.$store.dispatch('app/initScms', [mpItem.RESOURCENAME]);
              var scms = (this.$store.state.app.scms || {})[mpItem.RESOURCENAME] || [];
              this.fieldMapSourceFields = scms.filter(function(f) {
                return f.FIELDNAME;
              }).map(function(f) {
                return { key: f.FIELDNAME, title: f.FIELDNAME + (f.LABELNAME ? ' (' + f.LABELNAME + ')' : '') };
              });
            }
          }
        }
      }
      // 加载目标字段（子表资源）
      if (targetPath) {
        var curModData = this.$store.state.app.modules[this.moduleCode];
        if (curModData && curModData.MODPATH) {
          var tgtMpItem = curModData.MODPATH.find(function(p) { return p.PATHNAME === targetPath });
          if (tgtMpItem && tgtMpItem.RESOURCENAME) {
            this.fieldMapTargetName = tgtMpItem.RESOURCENAME;
            // eslint-disable-next-line no-restricted-syntax
            await this.$store.dispatch('app/initScms', [tgtMpItem.RESOURCENAME]);
            var tgtScms = (this.$store.state.app.scms || {})[tgtMpItem.RESOURCENAME] || [];
            this.fieldMapTargetFields = tgtScms.filter(function(f) {
              return f.FIELDNAME;
            }).map(function(f) {
              return { key: f.FIELDNAME, title: f.FIELDNAME + (f.LABELNAME ? ' (' + f.LABELNAME + ')' : '') };
            });
          }
        }
      }
      // 解析已有 FIELDMAP 到行
      this.fieldMapRows = [];
      var existing = this.btnForm.FIELDMAP || '';
      if (existing) {
        var pairs = existing.split(',');
        for (var i = 0; i < pairs.length; i++) {
          var kv = pairs[i].split('=');
          if (kv.length === 2) {
            this.fieldMapRows.push({ source: kv[0].trim(), target: kv[1].trim() });
          }
        }
      }
      if (this.fieldMapRows.length === 0 && this.fieldMapSourceFields.length > 0 && this.fieldMapTargetFields.length > 0) {
        this.fieldMapRows.push({ source: '', target: '' });
      }
      this.fieldMapModalVisible = true;
    },
    addFieldMapRow() {
      this.fieldMapRows.push({ source: '', target: '' });
    },
    removeFieldMapRow(ri) {
      this.fieldMapRows.splice(ri, 1);
    },
    applyFieldMap() {
      var parts = [];
      this.fieldMapRows.forEach(function(row) {
        if (row.source && row.target) {
          parts.push(row.source + '=' + row.target);
        }
      });
      this.btnForm.FIELDMAP = parts.join(',');
      this.fieldMapModalVisible = false;
    },
    // 跳转到扩展 JS 编辑器，自动合并插入钩子方法骨架
    openHookEditor(field) {
      var formKey = '';
      if (field === 'beforeAction') formKey = 'BEFOREACTION';
      else if (field === 'afterAction') formKey = 'AFTERACTION';
      else if (field === 'showCond') formKey = 'SHOWCOND';
      else if (field === 'paramsFn') formKey = 'EXTRAPARAMS';
      if (!formKey) return;
      var fieldValue = this.btnForm[formKey] || '';
      var methodName = '';
      if (field === 'paramsFn') {
        // 额外参数：空或合法 JSON → 生成方法名覆盖；已是方法名 → 沿用
        var isJson = false;
        if (fieldValue) {
          try {
            var t = JSON.parse(fieldValue);
            if (t && typeof t === 'object' && !Array.isArray(t)) isJson = true;
          } catch (e) { isJson = false }
        }
        methodName = (!fieldValue || isJson) ? this.genHookMethodName(field) : fieldValue.trim();
      } else if (field === 'showCond') {
        // 显隐：空或非标识符（表达式）→ 生成 ISSHOWXXX；已是标识符 → 沿用
        if (!fieldValue || !/^[A-Za-z_$][\w$]*$/.test(fieldValue.trim())) {
          methodName = this.genHookMethodName(field);
        } else {
          methodName = fieldValue.trim();
        }
      } else {
        methodName = fieldValue.trim() || this.genHookMethodName(field);
      }
      // 回填方法名到字段
      this.btnForm[formKey] = methodName;
      // 推导扩展 JS 路径（复用 openSfcEditor 的 extendjs 逻辑）
      var path = this.activePageConfigJson.EXTENDJS || '';
      if (!path && this.activePageConfig) {
        path = '@/modules/' + this.activeModuleCode + '/' + (this.activePageConfig.PAGECODE || 'form') + '.js';
      }
      var block = (field === 'showCond') ? 'computed' : 'methods';
      var snippet = this.buildHookSnippet(field, methodName);
      // 统一走模块脚本弹窗(code-editor-popup): 打开扩展 JS 并插入钩子方法骨架
      this.jsEditContext = 'btn_hook';
      this.$refs.codeEditorPopup.openJsInsert(path, this.activeModuleCode, {
        name: methodName,
        block: block,
        snippet: snippet,
        field: field
      });
    },
    onSfcEditorSaved(modulePath) {
      // 显式失效 SFC 缓存（清除该路径及所有变体），确保预览重新从 DB 加载最新代码
      if (modulePath) {
        invalidateCacheByPrefix(modulePath);
        // 同时清除带扩展名的变体 (tryExtensions 会尝试 .vue/.js 后缀)
        var dir = modulePath.substring(0, modulePath.lastIndexOf('/') + 1);
        invalidateCacheByPrefix(dir);
      }
      if (this.sfcEditorTarget === 'extendjs') {
        this.setActivePageConfigField('EXTENDJS', modulePath);
      } else if (this.sfcEditorTarget === 'sfcmodulepath') {
        this.setActivePageField('SFCMODULEPATH', modulePath);
      } else if (this.sfcEditorTarget === 'sub_sfcmodulepath') {
        this.subPageForm.SFCMODULEPATH = modulePath;
        this.$forceUpdate();
      } else if (this.sfcEditorTarget === 'modulestore') {
        this.refreshPreview();
      } else if (this.sfcEditorTarget === 'btn_hook') {
        // 按钮钩子/显隐/动态参数：方法名已在 openHookEditor 回填 btnForm，扩展 JS 内容变更无需回写
        this.refreshPreview();
      } else if (this.sfcEditorTarget === 'slot') {
        this.setSlotPath(this.sfcEditorSlotName, modulePath);
      }
    },
    // 子页面配置方法
    getSubPagesOf(page) {
      if (!page || !page.PAGECONFIG) return [];
      try {
        var json = JSON.parse(page.PAGECONFIG);
        if (!json.SUBPAGES || !Array.isArray(json.SUBPAGES)) return [];
        return json.SUBPAGES.filter(function(sp) { return sp.PAGEID || sp.REFMODULECODE });
      } catch (e) {
        return [];
      }
    },
    async selectSubPage(idx) {
      this.selectedSubIdx = idx;
      this.selectedSubIdx2 = null;
      // 确保引用模块已加载
      var spList = this.getSubPagesOf(this.currentPage);
      var sp = spList[idx];
      if (sp && sp.REFMODULECODE && !this.$store.state.app.modules[sp.REFMODULECODE]) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', sp.REFMODULECODE);
      }
    },
    async selectSubPage2(idx) {
      this.selectedSubIdx2 = idx;
      // 确保引用模块已加载
      var spList = this.getSubPagesOfSub(this.selectedSubIdx);
      var sp = spList[idx];
      if (sp && sp.REFMODULECODE && !this.$store.state.app.modules[sp.REFMODULECODE]) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', sp.REFMODULECODE);
      }
    },
    // 获取子页面的子页面（第二层嵌套）
    getSubPagesOfSub(subIdx) {
      var spList = this.getSubPagesOf(this.currentPage);
      var sp = spList[subIdx];
      if (!sp) return [];
      // 获取子页面的 pageConfig
      var subPageConfig = this.resolveSubPageConfig(sp);
      if (!subPageConfig) return [];
      return this.getSubPagesOf(subPageConfig);
    },
    resolveSubPageConfig(sp) {
      if (sp.REFMODULECODE) {
        var refData = this.$store.state.app.modules[sp.REFMODULECODE];
        if (refData && refData.MODPAGE) {
          return refData.MODPAGE.find(function(p) { return p.PAGECODE === sp.REFPAGECODE && (p.ISDELETED || 0) === 0 });
        }
      } else if (sp.PAGEID) {
        // 先从 app.modules 缓存查找
        var modData = this.$store.state.app.modules[this.moduleCode];
        if (modData && modData.MODPAGE) {
          var found = modData.MODPAGE.find(function(p) { return p.ID === sp.PAGEID && (p.ISDELETED || 0) === 0 });
          if (found) return found;
        }
        // 再从 DataTable 查找（新建的子页面记录还未同步到 app.modules 缓存）
        var dt = this.$MODPAGE;
        if (dt && dt.data) {
          var dtFound = dt.data.find(function(p) { return p.ID === sp.PAGEID });
          if (dtFound) return dtFound;
        }
      }
      return null;
    },
    openAddSubPage() {
      this.subPageModalMode = 'add';
      this.subPageEditIdx = -1;
      this.subPageForm = Object.assign({}, SUB_PAGE_FORM_DEFAULTS);
      this.$refs.subPageModal.show();
    },
    openRefSubPage() {
      this.subPageModalMode = 'ref';
      this.subPageEditIdx = -1;
      this.loadRefModules();
      this.subPageForm = Object.assign({}, SUB_PAGE_FORM_DEFAULTS);
      this.$refs.subPageModal.show();
    },
    openEditSubPage(idx) {
      var spList = this.getSubPagesOf(this.currentPage);
      var sp = spList[idx];
      if (!sp) return;
      this.subPageModalMode = sp.REFMODULECODE ? 'ref' : 'edit';
      this.subPageEditIdx = idx;
      this.subPageForm = Object.assign({}, sp);
      // 从 MODPAGE 记录获取 PAGECODE（SUBPAGES 中不存 PAGECODE，只在 tss_module_page 上）
      if (!sp.REFMODULECODE && sp.PAGEID) {
        var subPageConfig = this.resolveSubPageConfig(sp);
        if (subPageConfig) {
          this.subPageForm.PAGECODE = subPageConfig.PAGECODE || '';
        }
      }
      this.$refs.subPageModal.show();
      if (sp.REFMODULECODE) {
        this.loadRefModules();
        this.loadRefPages(sp.REFMODULECODE);
      }
    },
    removeSubPage(idx) {
      var json = this.pageConfigJson;
      if (!json.SUBPAGES) return;
      // getSubPagesOf 过滤后的索引 → 找到原始 SUBPAGES 中的索引
      var filtered = json.SUBPAGES.filter(function(sp) { return sp.PAGEID || sp.REFMODULECODE });
      var target = filtered[idx];
      if (!target) return;
      var rawIdx = json.SUBPAGES.indexOf(target);
      if (rawIdx >= 0) json.SUBPAGES.splice(rawIdx, 1);
      var dt = this.$MODPAGE;
      if (dt) dt.setValue('PAGECONFIG', JSON.stringify(json), this.currentPage);
      this.syncToAppModules(this.currentPage, 'PAGECONFIG', JSON.stringify(json));
      this.selectedSubIdx = null;
      this.selectedSubIdx2 = null;
      this.refreshPreview();
    },
    removeSubPage2(parentSubIdx, idx) {
      // 删除嵌套子页面：先找到父子页面的 pageConfig，再修改其 SUBPAGES
      var spList = this.getSubPagesOf(this.currentPage);
      var parentSp = spList[parentSubIdx];
      if (!parentSp) return;
      var parentConfig = this.resolveSubPageConfig(parentSp);
      if (!parentConfig || !parentConfig.PAGECONFIG) return;
      try {
        var json = JSON.parse(parentConfig.PAGECONFIG);
        if (!json.SUBPAGES) return;
        var filtered = json.SUBPAGES.filter(function(sp) { return sp.PAGEID || sp.REFMODULECODE });
        var target = filtered[idx];
        if (!target) return;
        var rawIdx = json.SUBPAGES.indexOf(target);
        if (rawIdx >= 0) json.SUBPAGES.splice(rawIdx, 1);
        var dt = this.$MODPAGE;
        if (dt) dt.setValue('PAGECONFIG', JSON.stringify(json), parentConfig);
        this.syncToAppModules(parentConfig, 'PAGECONFIG', JSON.stringify(json));
        this.selectedSubIdx2 = null;
        this.refreshPreview();
      } catch (e) {}
    },
    confirmSubPageModal() {
      var json = this.pageConfigJson;
      if (!json.SUBPAGES) json.SUBPAGES = [];

      // 自定义模式：先创建 MODPAGE 记录
      if (this.subPageModalMode === 'add') {
        var pageId = this._genId();
        var compType = this.subPageForm.COMPONENTTYPE || 'standard';
        var pageCode = this.subPageForm.PAGECODE || ('sub_' + Date.now());
        this.$store.commit(Constants.STORE_NAME + '/ADD', {
          path: 'MODPAGE',
          item: {
            ID: pageId,
            MODULECODE: this.moduleCode,
            PAGECODE: pageCode,
            PAGENAME: this.subPageForm.PAGENAME,
            PAGETYPE: this.subPageForm.PAGETYPE || 'form',
            PARENTID: this.currentPage.ID,
            COMPONENTTYPE: compType,
            SFCMODULEPATH: compType === 'sfc' ? (this.subPageForm.SFCMODULEPATH || '') : '',
            ISDELETED: 0,
            SORTNO: this.pages.length + 1
          }
        });
        this.subPageForm.PAGEID = pageId;

        // 根据 PAGETYPE 模板自动创建默认按钮（与主页面新增一致）
        var subPageType = this.subPageForm.PAGETYPE || 'form';
        var subTpl = PAGE_TPL_DEFAULTS[subPageType];
        if (subTpl && subTpl.buttons && subTpl.buttons.length > 0) {
          var subBtnDt = this.$MODBUTTON;
          if (subBtnDt) {
            var self3 = this;
            subTpl.buttons.forEach(function(btn) {
              self3.$store.commit(Constants.STORE_NAME + '/ADD', {
                path: 'MODBUTTON',
                item: Object.assign({}, btn, {
                  ID: self3._genId(),
                  PAGEID: pageId,
                  MODULECODE: self3.moduleCode,
                  ISDELETED: 0
                })
              });
            });
          }
        }
      }

      // 编辑模式：同步更新 MODPAGE 记录的 PAGECODE/PAGENAME
      if (this.subPageModalMode === 'edit' && this.subPageForm.PAGEID) {
        var subPageRecord = this.resolveSubPageConfig(this.subPageForm);
        if (subPageRecord) {
          var modDt = this.$MODPAGE;
          if (modDt) {
            if (this.subPageForm.PAGECODE) modDt.setValue('PAGECODE', this.subPageForm.PAGECODE, subPageRecord);
            if (this.subPageForm.PAGENAME) modDt.setValue('PAGENAME', this.subPageForm.PAGENAME, subPageRecord);
            if (this.subPageForm.PAGETYPE) modDt.setValue('PAGETYPE', this.subPageForm.PAGETYPE, subPageRecord);
            modDt.setValue('COMPONENTTYPE', this.subPageForm.COMPONENTTYPE || 'standard', subPageRecord);
            modDt.setValue('SFCMODULEPATH', this.subPageForm.SFCMODULEPATH || '', subPageRecord);
          }
          this.syncToAppModules(subPageRecord, 'PAGECODE', this.subPageForm.PAGECODE || subPageRecord.PAGECODE);
        }
      }

      var item = {
        PAGEID: this.subPageForm.PAGEID || '',
        PAGENAME: this.subPageForm.PAGENAME || '',
        PAGETYPE: this.subPageForm.PAGETYPE || 'form',
        COMPONENTTYPE: this.subPageForm.COMPONENTTYPE || 'standard',
        SFCMODULEPATH: this.subPageForm.SFCMODULEPATH || '',
        REFMODULECODE: this.subPageForm.REFMODULECODE || '',
        REFPAGECODE: this.subPageForm.REFPAGECODE || '',
        MODALWIDTH: this.subPageForm.MODALWIDTH || null,
        MODALFULLSCREEN: !!this.subPageForm.MODALFULLSCREEN
      };

      if (this.subPageModalMode === 'edit' || this.subPageModalMode === 'ref') {
        if (this.subPageEditIdx >= 0) {
          // 过滤后索引 → 原始 SUBPAGES 索引
          var filtered = json.SUBPAGES.filter(function(sp) { return sp.PAGEID || sp.REFMODULECODE });
          var target = filtered[this.subPageEditIdx];
          if (target) {
            var rawIdx = json.SUBPAGES.indexOf(target);
            if (rawIdx >= 0) json.SUBPAGES[rawIdx] = item;
          } else {
            json.SUBPAGES.push(item);
          }
        } else {
          json.SUBPAGES.push(item);
        }
      } else {
        json.SUBPAGES.push(item);
      }

      var dt = this.$MODPAGE;
      if (dt) dt.setValue('PAGECONFIG', JSON.stringify(json), this.currentPage);
      this.syncToAppModules(this.currentPage, 'PAGECONFIG', JSON.stringify(json));
      if (this.subPageModalMode === 'add') {
        this.syncAllToAppModules();
      }
      this.subPageModalVisible = false;
      this.refreshPreview();
    },
    loadRefModules() {
      var modules = this.$store.state.app.modules || {};
      this.refModuleOptions = Object.keys(modules).map(function(k) {
        var m = modules[k];
        var modArr = m.MOD;
        var mod = Array.isArray(modArr) && modArr.length > 0 ? modArr[0] : (modArr || {});
        return { key: k, title: k + (mod.MODULENAME ? ' (' + mod.MODULENAME + ')' : '') };
      });
    },
    loadRefPages(moduleCode) {
      if (!moduleCode) { this.refPageOptions = []; return }
      var modData = this.$store.state.app.modules[moduleCode];
      if (!modData || !modData.MODPAGE) { this.refPageOptions = []; return }
      this.refPageOptions = modData.MODPAGE
        .filter(function(p) { return (p.ISDELETED || 0) === 0 })
        .map(function(p) { return { key: p.PAGECODE, title: p.PAGECODE + ' - ' + (p.PAGENAME || '') } });
    },
    // 选择引用页面后，自动同步被引用页面的 PAGETYPE 到 subPageForm
    onRefPageChange(pageCode) {
      if (!pageCode || !this.subPageForm.REFMODULECODE) return;
      var modData = this.$store.state.app.modules[this.subPageForm.REFMODULECODE];
      if (!modData || !modData.MODPAGE) return;
      var refPage = modData.MODPAGE.find(function(p) {
        return p.PAGECODE === pageCode && (p.ISDELETED || 0) === 0;
      });
      if (refPage) {
        this.subPageForm.PAGETYPE = refPage.PAGETYPE || 'form';
        this.subPageForm.COMPONENTTYPE = refPage.COMPONENTTYPE || 'standard';
        this.subPageForm.SFCMODULEPATH = refPage.SFCMODULEPATH || '';
      }
    },
    // 将 DataTable 编辑同步到 app.modules，使预览实时生效
    syncToAppModules(pageConfig, field, value) {
      if (!pageConfig) return;
      var mc = this.activeModuleCode || this.moduleCode;
      var modData = this.$store.state.app.modules[mc];
      if (!modData || !modData.MODPAGE) return;
      var target = modData.MODPAGE.find(function(p) { return p.ID === pageConfig.ID });
      if (target) {
        this.$set(target, field, value);
      }
    },
    // 同步按钮变更到 app.modules
    syncBtnToAppModules(btn, field, value) {
      if (!btn) return;
      var mc = this.activeModuleCode || this.moduleCode;
      var modData = this.$store.state.app.modules[mc];
      if (!modData || !modData.MODBUTTON) return;
      var target = modData.MODBUTTON.find(function(b) { return b.ID === btn.ID });
      if (target) {
        this.$set(target, field, value);
      }
    },
    // 同步整个 MODPAGE/MODBUTTON 数据到 app.modules（用于新增/删除等批量变更）
    syncAllToAppModules() {
      if (!this.moduleCode) return;
      var modData = this.$store.state.app.modules[this.moduleCode];
      if (!modData) return;
      var dtPage = this.$MODPAGE;
      if (dtPage && dtPage.data) {
        this.$set(modData, 'MODPAGE', dtPage.data.filter(function(p) { return (p.ISDELETED || 0) !== 1 }));
      }
      var dtBtn = this.$MODBUTTON;
      if (dtBtn && dtBtn.data) {
        this.$set(modData, 'MODBUTTON', dtBtn.data.filter(function(b) { return (b.ISDELETED || 0) !== 1 }));
      }
    },
    // 刷新预览面板
    refreshPreview() {
      this.$nextTick(() => {
        if (this.$refs.pagePreview) {
          this.$refs.pagePreview.refresh();
        }
      });
    }
  }
};
</script>

<style lang="less" scoped>
.mod-config-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #F0F2F5;
}
.mod-config-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
  height: 50px;
}
.mod-config-toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.mod-config-back-btn {
  background: none;
  border: 1px solid #d9d9d9;
  border-radius: 6px;
  padding: 4px 12px;
  cursor: pointer;
  font-size: 13px;
  color: #434343;
  display: flex;
  align-items: center;
  gap: 4px;
  &:hover { color: #2F54EB; border-color: #2F54EB; }
}
.mod-title-label {
  color: #1d39c4;
  font-weight: 600;
  margin-right: 10px;
  font-size: 16px;
}
.mod-config-title {
  font-size: 16px;
  font-weight: 600;
  color: #1F1F1F;
}
.mod-config-toolbar-right {
  display: flex;
  gap: 8px;
  margin-right: 10px;
}
.mod-config-body {
  display: flex;
  flex: 1;
  overflow: hidden;
  margin: 5px 5px;
}
.mod-config-left {
  flex-shrink: 0;
  border-right: 1px solid #f0f0f0;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  background: #fff;
  padding: 5px;
  margin: 0 5px;
}
.mod-config-right {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  margin: 0 5px;
}

/* 分隔条 */
.mc-resizer-v {
  width: 4px;
  cursor: col-resize;
  background: #e8e8e8;
  border-radius: 2px;
  flex-shrink: 0;
  transition: background .15s;
  &:hover, &.mc-resizing { background: #1d39c4; }
}
.mc-resizer-h {
  height: 4px;
  cursor: row-resize;
  background: #e8e8e8;
  border-radius: 2px;
  flex-shrink: 0;
  transition: background .15s;
  &:hover, &.mc-resizing { background: #1d39c4; }
}

/* page list */
.mc-page-list {
  overflow-y: auto;
  max-height: 200px;
  border-bottom: 1px solid #f0f0f0;
}
.mc-page-item {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  gap: 10px;
  transition: background 0.2s;
  &:hover { background: #F0F5FF; }
}
.mc-page-item-active {
  background: #E6F4FF !important;
}
.mc-page-icon {
  width: 20px;
  text-align: center;
  font-size: 14px;
  flex-shrink: 0;
}
.mc-pt-list { color: #2F54EB; }
.mc-pt-form { color: #52C41A; }
.mc-pt-review { color: #FAAD14; }
.mc-pt-report { color: #8C8C8C; }
.mc-page-name {
  flex: 1;
  font-size: 14px;
  color: #262626;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mc-page-type-tag {
  font-size: 12px;
  color: #8C8C8C;
  background: #F5F5F5;
  padding: 2px 8px;
  border-radius: 4px;
  flex-shrink: 0;
}
.mc-page-del {
  background: none;
  border: none;
  cursor: pointer;
  color: #BFBFBF;
  padding: 4px 6px;
  flex-shrink: 0;
  font-size: 13px;
  min-width: 24px;
  min-height: 24px;
  &:hover { color: #F5222D; }
}
.mc-empty {
  padding: 20px;
  text-align: center;
  color: #BFBFBF;
  font-size: 14px;
}

/* props form */
.mc-props-form {
  padding: 0;
}
.mc-props-divider {
  font-size: 12px;
  color: #8C8C8C;
  padding: 6px 0 4px;
  border-top: 1px solid #f0f0f0;
  margin-top: 4px;
  font-weight: 600;
}
.h-form .h-form-item {
    padding-bottom: 10px;
    position: relative;
}

/* bottom half */
.mc-bottom {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-top: 1px solid #f0f0f0;
}
.mc-half {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-bottom: 1px solid #f0f0f0;
  min-height: 0;
}
.mc-half-body {
  flex: 1;
  overflow-y: auto;
  min-height: 0;
}

/* button list */
.mc-btn-body {
  padding: 0 8px;
}
.mc-btn-group {
  margin-top: 4px;
}
.mc-btn-group-title {
  font-size: 12px;
  color: #8C8C8C;
  padding: 4px 8px;
  font-weight: 600;
}
.mc-btn-row {
  display: flex;
  align-items: center;
  padding: 6px 8px;
  gap: 6px;
  border-bottom: 1px solid #f5f5f5;
  &:hover { background: #F0F5FF; }
}
.mc-btn-name {
  flex: 1;
  font-size: 13px;
  color: #262626;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mc-btn-tag {
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 3px;
  flex-shrink: 0;
}
.mc-btn-tag-btntype {
  color: #2F54EB;
  background: #F0F5FF;
}
.mc-btn-tag-btncode {
  color: #FA8C16;
  background: #FFF7E6;
}
.mc-btn-tag-api {
  color: #8C8C8C;
  background: #F5F5F5;
}
.mc-btn-act {
  cursor: pointer;
  color: #BFBFBF;
  padding: 4px 2px;
  flex-shrink: 0;
  font-size: 13px;
  min-width: 20px;
  min-height: 20px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  &:hover { color: #2F54EB; }
}
.mc-btn-act-del:hover { color: #F5222D; }
.mc-btn-act-disabled {
  color: #E0E0E0 !important;
  cursor: not-allowed;
  &:hover { color: #E0E0E0 !important; }
}
// 相邻动作按钮(上移/下移/编辑/删除)之间压紧, 避免占用过多宽度
.mc-btn-act + .mc-btn-act { margin-left: -4px; }

/* SFC Slot 扩展行 */
.mc-slot-row {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
}
.mc-slot-label {
  width: 100px;
  flex-shrink: 0;
  text-align: left;
  padding-right: 8px;
  color: #666;
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.mc-slot-value {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 0;
}

/* 按钮模板选择区 */
.mc-btn-tpl-section {
  margin-bottom: 12px;
}
.mc-btn-tpl-label {
  font-size: 13px;
  color: #595959;
  margin-bottom: 8px;
  font-weight: 600;
}
.mc-btn-tpl-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.mc-btn-tpl-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 12px;
  border: 1px solid #D9D9D9;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
  color: #434343;
  background: #fff;
  transition: all 0.2s;
  &:hover {
    border-color: #2F54EB;
    color: #2F54EB;
    background: #F0F5FF;
  }
  i {
    font-size: 14px;
  }
}
.mc-btn-tpl-divider {
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px dashed #D9D9D9;
  font-size: 12px;
  color: #8C8C8C;
}

/* 快捷标签：input 下方可点击的预设值 */
.mc-quick-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: 4px;
}
.mc-quick-tag {
  display: inline-block;
  font-size: 11px;
  padding: 1px 6px;
  border: 1px solid #D9D9D9;
  border-radius: 3px;
  color: #595959;
  background: #FAFAFA;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s;
  &:hover {
    color: #2F54EB;
    border-color: #2F54EB;
    background: #F0F5FF;
  }
  i {
    font-size: 11px;
    margin-right: 2px;
  }
}

/* fullscreen */
.mod-config-page:fullscreen,
.mod-config-page:-webkit-full-screen {
  width: 100vw;
  height: 100vh;
  background: #F0F2F5;
}

/* 子页面配置 */
.mc-subpage-item {
  display: flex;
  align-items: center;
  padding: 7px 16px 7px 36px;
  gap: 8px;
  border-bottom: 1px solid #f5f5f5;
  font-size: 13px;
  cursor: pointer;
  transition: background 0.2s;
  &:hover { background: #F0F5FF; }
}
.mc-subpage-l1 {
  padding-left: 36px;
}
.mc-subpage-l2 {
  padding-left: 56px;
}
.mc-subpage-item-active {
  background: #E6F4FF !important;
}
.mc-subpage-icon {
  color: #8C8C8C;
  font-size: 12px;
  flex-shrink: 0;
  width: 16px;
  text-align: center;
}
.mc-subpage-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: #595959;
}
.mc-subpage-tag {
  font-size: 11px;
  padding: 1px 6px;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  color: #8c8c8c;
  background: #fafafa;
  flex-shrink: 0;
}
.mc-subpage-del {
  flex-shrink: 0;
}
.mc-subpage-actions {
  display: flex;
  gap: 8px;
  padding: 6px 16px;
  border-bottom: 1px solid #f0f0f0;
}
.mc-param-mapping-row {
  display: flex; align-items: center; gap: 6px; margin-bottom: 4px;
}
.mc-param-arrow { color: #8c8c8c; }
.mc-sfc-path-tag {
  font-size: 11px; padding: 1px 6px; border: 1px solid #d9d9d9;
  border-radius: 3px; color: #515a6e; background: #f7f7f7;
  max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.mc-sfc-path-empty {
  font-size: 12px; color: #c5c8ce;
}
/* 发布弹窗 */
.publish-input {
  width: 100%;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  padding: 4px 8px;
  font-size: 13px;
  outline: none;
  &:focus { border-color: #0a84ff; }
}
.publish-tip {
  margin-top: 8px;
  padding: 8px 10px;
  background: #f5f7fa;
  border-left: 3px solid #0a84ff;
  font-size: 12px;
  color: #666;
  line-height: 1.6;
}
.publish-em { color: #0a84ff; font-weight: 600; }
.codefiles-tip {
  color: #9ea7b4;
  font-size: 12px;
  padding-bottom: 10px;
}
.codefiles-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  th, td {
    border-bottom: 1px solid #f0f0f0;
    padding: 7px 8px;
    text-align: left;
  }
  th {
    background: #f8f8f9;
    color: #515a6e;
    font-size: 12px;
  }
  .codefiles-kind {
    padding: 1px 6px;
    border-radius: 3px;
    font-size: 11px;
    font-weight: bold;
    &.csharp { color: #9b59b6; border: 1px solid #9b59b6; }
    &.sql { color: #16a085; border: 1px solid #16a085; }
  }
  .codefiles-edit { color: #2d8cf0; cursor: pointer; }
  .codefiles-empty {
    text-align: center;
    color: #9ea7b4;
    padding: 20px 0;
  }
}

/* 字段映射弹窗 */
.field-map-config { padding: 4px 0; }
.field-map-info { font-size: 13px; color: #666; margin-bottom: 12px; }
.field-map-info b { color: #303133; }
.field-map-empty { color: #999; font-size: 13px; padding: 20px 0; text-align: center; }
.field-map-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; font-size: 12px; color: #999; font-weight: 600; }
.field-map-row { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.field-map-col { flex: 1; }
.field-map-arrow { flex-shrink: 0; width: 20px; text-align: center; color: #999; font-weight: bold; }
.field-map-act { flex-shrink: 0; width: 24px; text-align: center; cursor: pointer; color: #999; }
.field-map-act:hover { color: #f5222d; }
</style>
