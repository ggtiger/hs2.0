<template>
  <div>
    <Uploader
      @fileclick="fileclick"
      :type="type"
      :files="fileList"
      :data-type="dataType"
      :limit="limit"
      :uploadList="uploadList"
      ref="uploader"
      :dragdrop="dragdrop"
      :class-name="className"
      @deletefile="deletefile"
      :readonly="readonly"
    >
      <div slot="dragdrop" v-if="$slots.dragdrop">
        <slot name="dragdrop"></slot>
      </div>
    </Uploader>
  </div>
</template>
<script>
// import qiniujs from 'plupload-es6';
import initUploader from './uploader';
import db from '@/api/db';
export default {
  name: 'rs-uploader',
  props: {
    options: {
      type: Object,
      default: () => {},
    },
    type: {
      type: String,
      default: 'image',
    },
    dataType: String,
    dragdrop: {
      type: Boolean,
      default: false,
    },
    value: {
      type: [Object, Array, String],
    },
    limit: Number,
    className: String,
    readonly: {
      type: Boolean,
      default: false,
    },
  },
  data() {
    return {
      uploadList: [],
      fileList: null,
    };
  },
  watch: {
    value: {
      handler: function() {
        if (this.type === 'images' || this.type === 'files') {
          this.fileList = [];
          this.fileList = this.value || [];
          this.fileList.map(v => {
            v.url = `${db.getUrl('upload')}${v.id}`;
          });
          return this.fileList;
        } else {
          this.fileList = '';
          if (this.value) {
            this.fileList = this.value || {};
            this.fileList.url = `${db.getUrl('upload')}${this.value.id}`;
          }
        }
      },
      immediate: true
    },
  },
  methods: {
    getShowFiles() {
      if (this.type === 'images' || this.type === 'files') {
        this.value = this.value || [];
        this.value.map(v => {
          v.url = `${db.getUrl('upload')}${v.id}`;
        });
        return [...this.value];
      } else {
        if (this.value) this.value.url = `${db.getUrl('upload')}${this.value.id}`;
      }
      return this.value;
    },
    deletefile(index) {
      let value = null;
      if (this.type === 'images' || this.type === 'files') {
        value = [...this.fileList];
        value.splice(index, 1);
      } else {
        value = {};
        this.uploadList = [];
      }
      this.fileList = value;
      this.$emit('input', value);
    },
    init() {
      initUploader({
        headers: { Authorization: 'Bearer ' + this.$store.state['user'].access_token },
        uptoken_url: db.getUrl('upload'),
        browserButton: this.$refs.uploader.getBrowseButton(),
        url: db.getUrl('upload'),
        fnFilesAdded: (up, files) => {
          files.forEach(file => {
            if (FileReader) {
              let reader = new FileReader();
              reader.onload = event => {
                file.thumbUrl = event.target.result;
              };
              reader.readAsDataURL(file.getNative());
            }
            if (this.type === 'files' || this.type === 'images') {
              this.uploadList.push(file);
            } else {
              this.uploadList = [file];
            }
          });
        },
        fnUploadComplete: () => {
          // this.$emit('completeUpload');
          // let fileList = this.$refs.uploader.getFileList();
          if (this.type === 'files' || this.type === 'images') {
            // this.uploadList.splice(0, this.uploadList.length);
            // this.$emit('input', fileList);
          } else {
            // this.$emit('input', fileList);
          }
        },
        fnFileUploaded: (up, file, res) => {
          file.id = res.id;
          if (this.type === 'files' || this.type === 'images') {
            let files = this.fileList;
            files.push({ id: res.id, name: file.name, url: `${db.getUrl('upload')}${res.id}` });
            this.uploadList = [];
            this.$emit('input', this.fileList);
          } else {
            this.$emit('input', { id: res.id, name: file.name, url: `${db.getUrl('upload')}${res.id}` });
          }
        },
        fnError: (up, err, errTip) => {
          this.$error(errTip);
        },
      });
    },
    fileclick(file) {
      window.open(`${db.getUrl('upload')}${file.id || file.original.id}`, '_black');
    },
  },
  mounted() {
    this.$nextTick(() => {
      this.init();
    });
  },
};
</script>
<style lang="postcss" scoped>
/deep/ .h-uploader-image,
.h-uploader-image-empty {
  position: relative;
  float: left;
  height: 70px;
  width: 70px;
  display: inline-block;
  border-radius: 4px;
  margin-right: 10px;
}
</style>
