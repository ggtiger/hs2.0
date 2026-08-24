<template>
  <list-t01
    title="LLM用量"
    :bcDatas="datas"
    :store="store"
  >
  </list-t01>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';

export default {
  name: 's01-m15-main',
  data() {
    return {
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: '系统管理' }, { title: 'LLM用量' }]
    };
  }
};
</script>
