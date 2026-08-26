<template>
  <div class="logistics-page">
    <div class="logistics-header">
      <h1>物流查询</h1>
      <p class="logistics-subtitle">睿谱希 - 物流追踪服务</p>
    </div>
    <div class="logistics-search">
      <input
        type="text"
        v-model="expNo"
        placeholder="请输入物流单号"
        class="search-input"
        @keyup.enter="doSearch"
      />
      <button class="search-btn" @click="doSearch">查询</button>
    </div>
    <div class="logistics-result" v-if="result">
      <div class="result-info">
        <div class="info-row">
          <span class="info-label">快递公司：</span>
          <span class="info-value">{{ result.LOGISTICS_COMPANY }}</span>
        </div>
        <div class="info-row">
          <span class="info-label">物流单号：</span>
          <span class="info-value">{{ result.LOGISTICS_NO }}</span>
        </div>
      </div>
      <div class="timeline" v-if="result.TRACKS && result.TRACKS.length > 0">
        <h3>物流轨迹</h3>
        <ul class="timeline-list">
          <li class="timeline-item" v-for="(track, index) in result.TRACKS" :key="index" :class="{ active: index === 0 }">
            <div class="timeline-dot"></div>
            <div class="timeline-content">
              <div class="timeline-time">{{ track.TRACK_TIME }}</div>
              <div class="timeline-desc">{{ track.TRACK_DESC }}</div>
              <div class="timeline-photo" v-if="track.TRACK_PHOTO">
                <img :src="track.TRACK_PHOTO" alt="物流节点照片" />
              </div>
            </div>
          </li>
        </ul>
      </div>
      <div class="no-result" v-else>
        <p>暂无物流轨迹信息</p>
      </div>
    </div>
    <div class="logistics-empty" v-if="searched && !result">
      <p>未找到相关物流信息，请确认物流单号是否正确</p>
    </div>
    <div class="logistics-footer">
      <p>睿谱希管理系统</p>
    </div>
  </div>
</template>

<script>
import { queryByExpNo } from './store';
export default {
  name: 'out-logistics',
  data() {
    return {
      expNo: '',
      result: null,
      searched: false,
    };
  },
  methods: {
    async doSearch() {
      if (!this.expNo.trim()) return;
      this.searched = false;
      this.result = null;
      try {
        let ret = await queryByExpNo({ expNo: this.expNo.trim() });
        if (ret) {
          this.result = ret;
        }
      } catch (e) {
        // ignore
      }
      this.searched = true;
    },
  },
};
</script>

<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
.logistics-page {
  max-width: 700px;
  margin: 0 auto;
  padding: 40px 20px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  color: #333;
}
.logistics-header {
  text-align: center;
  margin-bottom: 30px;
}
.logistics-header h1 {
  font-size: 28px;
  color: #1890ff;
  margin-bottom: 8px;
}
.logistics-subtitle {
  color: #999;
  font-size: 14px;
}
.logistics-search {
  display: flex;
  justify-content: center;
  margin-bottom: 30px;
}
.search-input {
  width: 360px;
  height: 40px;
  border: 1px solid #d9d9d9;
  border-radius: 4px 0 0 4px;
  padding: 0 12px;
  font-size: 14px;
  outline: none;
}
.search-input:focus {
  border-color: #1890ff;
}
.search-btn {
  height: 40px;
  padding: 0 24px;
  background: #1890ff;
  color: #fff;
  border: none;
  border-radius: 0 4px 4px 0;
  font-size: 14px;
  cursor: pointer;
}
.search-btn:hover {
  background: #40a9ff;
}
.result-info {
  background: #f5f7fa;
  border-radius: 6px;
  padding: 20px;
  margin-bottom: 24px;
}
.info-row {
  display: flex;
  align-items: center;
  padding: 6px 0;
}
.info-label {
  color: #999;
  width: 90px;
  flex-shrink: 0;
}
.info-value {
  color: #333;
}
.timeline h3 {
  margin-bottom: 16px;
  font-size: 16px;
  color: #333;
}
.timeline-list {
  list-style: none;
  padding-left: 20px;
  border-left: 2px solid #e8e8e8;
}
.timeline-item {
  position: relative;
  padding: 0 0 24px 20px;
}
.timeline-item:last-child {
  padding-bottom: 0;
}
.timeline-dot {
  position: absolute;
  left: -27px;
  top: 4px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #d9d9d9;
  border: 2px solid #fff;
}
.timeline-item.active .timeline-dot {
  background: #1890ff;
}
.timeline-time {
  color: #999;
  font-size: 13px;
  margin-bottom: 4px;
}
.timeline-desc {
  color: #333;
  font-size: 14px;
}
.timeline-photo {
  margin-top: 8px;
}
.timeline-photo img {
  max-width: 200px;
  max-height: 150px;
  border-radius: 4px;
  border: 1px solid #e8e8e8;
}
.logistics-empty {
  text-align: center;
  padding: 40px 0;
  color: #999;
}
.logistics-footer {
  text-align: center;
  margin-top: 40px;
  color: #ccc;
  font-size: 12px;
}
</style>
