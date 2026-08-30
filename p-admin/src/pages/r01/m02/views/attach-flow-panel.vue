<template>
  <div class="rr-afp" :class="{'rr-afp-wide': wide}">
    <!-- 宽屏：三栏 附件左 表单中 审批右 -->
    <template v-if="wide">
      <div class="rr-afp-attach">
        <div class="rr-afp-title">附件列表</div>
        <RsUploader
          v-if="canUpload"
          :readonly="readonly"
          type="files"
          data-type="file"
          :value="files"
          @input="$emit('input', $event)"
          class="rr-hide-default-uploader"
        ></RsUploader>
        <div class="rr-afp-grid">
          <div class="rr-afp-item" v-for="(file, index) in files" :key="index" v-show="isImg(file) || isVideo(file)">
            <div class="rr-afp-preview" v-if="isImg(file)" @mouseenter="onImgEnter($event, file, index)" @mouseleave="hoverIdx = -1">
              <a :href="getUrl(file)" target="_blank" class="rr-afp-img-link">
                <img :src="getUrl(file)" class="rr-afp-thumb" />
              </a>
              <i class="h-icon-trash rr-afp-del" v-if="!readonly" @click="onDel(index)"></i>
            </div>
            <div class="rr-afp-preview rr-afp-video" v-else-if="isVideo(file)" @mouseenter="hoverIdx = index" @mouseleave="hoverIdx = -1">
              <video :src="getUrl(file)" class="rr-afp-thumb" muted></video>
              <div class="rr-afp-play"></div>
              <div class="rr-afp-hover-mask" v-show="hoverIdx === index" @click="openVideo(file)">
                <i class="h-icon-search"></i>
              </div>
              <i class="h-icon-trash rr-afp-del" v-if="!readonly" @click="onDel(index)"></i>
            </div>
          </div>
        </div>
        <div class="rr-afp-list">
          <div class="rr-afp-list-row" v-for="(file, index) in files" :key="'f'+index" v-show="!isImg(file) && !isVideo(file)">
            <i class="h-icon-file" :class="getIcon(file)"></i>
            <a class="rr-afp-link" :href="getUrl(file)" target="_blank" :title="file.name">{{ file.name }}</a>
            <i class="h-icon-trash rr-afp-del-text" v-if="!readonly" @click="onDel(index)"></i>
          </div>
        </div>
      </div>
      <div class="rr-afp-form">
        <slot></slot>
      </div>
      <div class="rr-afp-flow">
        <div class="rr-afp-title">审批记录</div>
        <div class="rr-afp-timeline" v-if="logs.length > 0">
          <div class="rr-afp-flow-item" v-for="(item, index) in logs" :key="index">
            <div class="rr-afp-flow-dot" :class="getDotClass(item.STATE)"></div>
            <div class="rr-afp-flow-line" v-if="index < logs.length - 1"></div>
            <div class="rr-afp-flow-body">
              <div class="rr-afp-flow-head">
                <span class="rr-afp-flow-user">{{item.OPLOGER}}</span>
                <span class="rr-afp-flow-state" :class="getStateClass(item.STATE)">{{item.STATE}}</span>
              </div>
              <div class="rr-afp-flow-time">{{item.OPLOGDATE}}</div>
              <div class="rr-afp-flow-remark" v-if="item.REMARK">{{item.REMARK}}</div>
            </div>
          </div>
        </div>
      </div>
    </template>
    <!-- 窄屏：原始布局 -->
    <template v-else>
      <slot></slot>
      <div class="rr-afp-narrow">
        <div>
          附件列表
          <RsUploader
            :readonly="readonly"
            type="files"
            data-type="file"
            :value="files"
            @input="$emit('input', $event)"
          ></RsUploader>
        </div>
        <div class="rr-afp-narrow-flow" v-if="logs.length > 0">
          审批记录
          <div>
            <table>
              <tr v-for="(item, index) in logs" :key="index">
                <td width="100px;">{{item.OPLOGER}}</td>
                <td width="200px;">{{item.OPLOGDATE}}</td>
                <td width="100px;">{{item.STATE}}</td>
                <td>{{item.REMARK}}</td>
              </tr>
            </table>
          </div>
        </div>
      </div>
    </template>
    <!-- 图片悬浮大图 -->
    <div class="rr-afp-popup" v-show="hoverIdx >= 0 && popupUrl" :style="popupStyle" @mouseenter="hoverIdx = -1">
      <img :src="popupUrl" />
    </div>
    <!-- 视频预览弹窗 -->
    <Modal v-model="showVideo" :hasCloseIcon="true" :width="800">
      <div slot="header">视频预览</div>
      <div style="background:#000;display:flex;align-items:center;justify-content:center;min-height:400px;border-radius:6px;overflow:hidden;">
        <video :src="videoUrl" style="max-width:100%;max-height:70vh;" controls autoplay></video>
      </div>
    </Modal>
  </div>
</template>

<script>
import RsUploader from '@/components/rs-uploader';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';

export default {
  name: 'attach-flow-panel',
  components: { RsUploader },
  props: {
    files: { type: Array, default: () => [] },
    logs: { type: Array, default: () => [] },
    wide: { type: Boolean, default: false },
    readonly: { type: Boolean, default: false },
    canUpload: { type: Boolean, default: true },
  },
  data() {
    return {
      hoverIdx: -1,
      popupUrl: '',
      popupStyle: {},
      showVideo: false,
      videoUrl: '',
    };
  },
  methods: {
    isImg(f) {
      var ext = this.ext(f.name);
      return ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(ext);
    },
    isVideo(f) {
      var ext = this.ext(f.name);
      return ['mp4', 'webm', 'ogg', 'mov', 'avi', 'mkv'].includes(ext);
    },
    ext(name) {
      if (!name) return '';
      var parts = name.split('.');
      return parts.length > 1 ? parts.pop().toLowerCase() : '';
    },
    getUrl(f) {
      return `${db.getUrl('upload')}${f.id}`;
    },
    getIcon(f) {
      var e = this.ext(f.name);
      var m = {
        pdf: 'ri-pdf',
        doc: 'ri-doc',
        docx: 'ri-doc',
        xls: 'ri-xls',
        xlsx: 'ri-xls',
        ppt: 'ri-ppt',
        pptx: 'ri-ppt',
        zip: 'ri-zip',
        rar: 'ri-zip',
        '7z': 'ri-zip',
        txt: 'ri-txt',
      };
      return m[e] || 'ri-file';
    },
    getDotClass(state) {
      var m = { '已提交': 'dot-blue', '已审核': 'dot-green', '已审批': 'dot-green', '已驳回': 'dot-red', '已签发': 'dot-primary', '已作废': 'dot-gray' };
      return m[state] || 'dot-blue';
    },
    getStateClass(state) {
      var m = { '已提交': 'st-blue', '已审核': 'st-green', '已审批': 'st-green', '已驳回': 'st-red', '已签发': 'st-primary', '已作废': 'st-gray' };
      return m[state] || 'st-blue';
    },
    onImgEnter(e, file, index) {
      this.hoverIdx = index;
      this.popupUrl = this.getUrl(file);
      var rect = e.target.getBoundingClientRect();
      this.popupStyle = { left: (rect.right + 8) + 'px', top: rect.top + 'px' };
    },
    openVideo(file) {
      this.videoUrl = this.getUrl(file);
      this.showVideo = true;
    },
    onDel(index) {
      this.$emit('remove', index);
    },
  },
};
</script>

<style lang="less" scoped>
.rr-afp {
  width: 100%;
  // 隐藏默认 Uploader 的文件列表
  .rr-hide-default-uploader {
    /deep/ .h-uploader-files { display: none; }
    /deep/ .h-uploader-file-list { display: none; }
  }
}
.rr-afp-title {
  font-size: 15px;
  font-weight: 600;
  color: #1F1F1F;
  margin-bottom: 12px;
  padding-left: 10px;
  border-left: 3px solid #2F54EB;
}
// ===== 宽屏三栏 =====
.rr-afp-wide {
  display: flex;
  gap: 20px;
}
.rr-afp-attach {
  width: 200px;
  flex-shrink: 0;
  border-right: 1px solid #f0f0f0;
  padding-right: 12px;
  overflow-y: auto;
}
.rr-afp-form {
  flex: 1;
  min-width: 0;
}
.rr-afp-flow {
  width: 200px;
  flex-shrink: 0;
  border-left: 1px solid #f0f0f0;
  padding-left: 12px;
}
// 网格
.rr-afp-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 6px;
}
.rr-afp-item {
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  overflow: visible;
  transition: all 0.2s;
  &:hover { border-color: #ADC6FF; box-shadow: 0 2px 8px rgba(47, 84, 235, 0.08); }
}
.rr-afp-preview {
  position: relative;
  height: 80px;
  background: #fafafa;
  .rr-afp-img-link { display: block; width: 100%; height: 100%; }
  .rr-afp-thumb { width: 100%; height: 100%; object-fit: cover; display: block; }
  .rr-afp-del {
    position: absolute; top: 4px; right: 4px; font-size: 14px; color: #fff;
    background: rgba(0,0,0,0.4); width: 22px; height: 22px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center; cursor: pointer;
    opacity: 0; transition: opacity 0.2s; z-index: 2;
    &:hover { background: rgba(245,34,45,0.8); }
  }
  &:hover .rr-afp-del { opacity: 1; }
  .rr-afp-hover-mask {
    position: absolute; inset: 0; background: rgba(0,0,0,0.4);
    display: flex; align-items: center; justify-content: center; color: #fff; font-size: 24px;
    i { background: rgba(255,255,255,0.2); width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center; }
  }
}
.rr-afp-video {
  .rr-afp-play {
    position: absolute; top: 50%; left: 50%; transform: translate(-50%,-50%);
    width: 36px; height: 36px; background: rgba(0,0,0,0.5); border-radius: 50%;
    pointer-events: none;
    &::after { content: ''; display: block; width: 0; height: 0; border-style: solid; border-width: 7px 0 7px 12px; border-color: transparent transparent transparent #fff; margin-left: 2px; }
  }
  &:hover .rr-afp-play { display: none; }
}
// 非图片列表
.rr-afp-list { margin-top: 6px; }
.rr-afp-list-row {
  display: flex; align-items: center; gap: 6px; padding: 6px 0; border-bottom: 1px solid #f5f5f5;
  i.h-icon-file {
    font-size: 18px; flex-shrink: 0;
    &.ri-pdf { color: #F5222D; } &.ri-doc { color: #2F54EB; } &.ri-xls { color: #52C41A; }
    &.ri-ppt { color: #FA8C16; } &.ri-zip { color: #722ED1; } &.ri-txt { color: #8C8C8C; }
    &.ri-file { color: #BFBFBF; }
  }
  .rr-afp-link {
    flex: 1; min-width: 0; font-size: 12px; color: #1890ff; text-decoration: none; cursor: pointer;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    &:hover { color: #40a9ff; text-decoration: underline; }
  }
  .rr-afp-del-text {
    font-size: 14px; color: #999; cursor: pointer; flex-shrink: 0;
    &:hover { color: #F5222D; }
  }
}
// 审批时间轴
.rr-afp-timeline { padding: 4px 0; }
.rr-afp-flow-item { position: relative; padding-left: 20px; padding-bottom: 20px; &:last-child { padding-bottom: 0; } }
.rr-afp-flow-dot {
  position: absolute; left: 0; top: 4px; width: 10px; height: 10px; border-radius: 50%; z-index: 1;
  &.dot-blue { background: #2F54EB; } &.dot-green { background: #52C41A; } &.dot-red { background: #F5222D; }
  &.dot-primary { background: #597EF7; } &.dot-gray { background: #BFBFBF; }
}
.rr-afp-flow-line { position: absolute; left: 4px; top: 16px; bottom: 0; width: 2px; background: #f0f0f0; }
.rr-afp-flow-head { display: flex; justify-content: space-between; align-items: center; }
.rr-afp-flow-user { font-weight: 600; color: #1F1F1F; font-size: 14px; }
.rr-afp-flow-state {
  font-size: 12px; font-weight: 500; padding: 1px 8px; border-radius: 10px;
  &.st-blue { color: #2F54EB; background: #F0F5FF; } &.st-green { color: #52C41A; background: #F6FFED; }
  &.st-red { color: #F5222D; background: #FFF1F0; } &.st-primary { color: #597EF7; background: #F0F5FF; }
  &.st-gray { color: #8C8C8C; background: #FAFAFA; }
}
.rr-afp-flow-time { color: #8C8C8C; font-size: 12px; margin-top: 4px; }
.rr-afp-flow-remark { color: #434343; margin-top: 6px; font-size: 13px; background: #F5F5F5; padding: 8px 12px; border-radius: 6px; border-left: 3px solid #2F54EB; }
// 图片悬浮大图
.rr-afp-popup {
  position: fixed; z-index: 9999; background: #fff; border: 1px solid #eee;
  border-radius: 6px; box-shadow: 0 4px 20px rgba(0,0,0,0.15); padding: 4px;
  img { display: block; max-width: 300px; max-height: 300px; border-radius: 4px; }
}
// ===== 窄屏 =====
.rr-afp-narrow-flow {
  margin-top: 16px;
  table { width: 100%; border-collapse: collapse; }
  td { padding: 6px 8px; border-bottom: 1px solid #f0f0f0; font-size: 13px; }
}
</style>
