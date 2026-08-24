import Pluploadjs from 'plupload-es6';
import store from '@/store';
export default function initUploader({
  perm = 'PRIVATE', // 公开 或者 私有
  browserButton,
  dragdropElement,
  url = '', // 上传服务器地址
  multiSelection = false,
  maxFileSize = '100mb',
  filters = {},
  params = {},
  headers = {},
  fnFilesAdded = () => { },
  fnBeforeUpload = () => { },
  fnUploadProgress = () => { },
  fnFileUploaded = () => { },
  fnUploadComplete = () => { },
  fnError = () => { },
}) {
  filters.max_file_size = maxFileSize;
  const domain = '';
  let param = {
    runtimes: 'html5',
    browse_button: browserButton,
    url: `${url}`,
    domain: '',
    chunk_size: '50kb',
    unique_names: true,
    multi_selection: multiSelection,
    filters,
    headers,
    init: {
      FilesAdded(up, files) {
        let result = fnFilesAdded(up, files);
        // 超过上传限制
        if (result === false) {
          return false;
        }
        up.fileLength = files.length;
        up.start();
      },
      BeforeUpload(up, file) {
        // 上传token设置
        // up.setOption('url', `${url}?token=${G.get('customUploadToken').value}&permission=${perm}`);
        // up.setOption();
        up.setOption('multipart_params', {
          _userInfo_: JSON.stringify(store.state['user'].userInfo),
          fileName: file.name
        });
        fnBeforeUpload(up, file);
      },
      UploadProgress(up, file) {
        fnUploadProgress(up, file);
      },
      FileUploaded(up, file, info) {
        const res = JSON.parse(info.response);
        up.fileLength -= 1;
        fnFileUploaded(up, file, res, `${domain}${res.key}`);
      },
      UploadComplete() {
        fnUploadComplete();
      },
      Error(up, err, errTip) {
        if (err.code === -600) {
          let max = up.settings.max_file_size || up.settings.filters.max_file_size;
          errTip = `文件大小不可超过${max.substring(0, max.length - 1).toUpperCase()}`;
        } else if (err.code === -601) {
          errTip = '文件格式不支持';
        } else {
          errTip = err.message;
        }

        fnError(up, err, errTip);
      },
    },
  };

  // 是否允许拖拽
  if (dragdropElement) {
    param.dragdrop = true;
    param.drop_element = dragdropElement;
  }
  param = { ...param, ...params };
  const uploader = new Pluploadjs.plupload.Uploader(param);
  uploader.init();
  return uploader;
}
