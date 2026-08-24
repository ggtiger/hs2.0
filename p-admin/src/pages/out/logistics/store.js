import db from '@/api/db';

// 外部对接页（物流查询），走标准 /api/data/call/R02_M07 通道
const STORE_NAME = 'out/logistics';

// 按 LOGISTICS_NO 查询物流（R02_M07/A10 RPC）
async function queryByExpNo({ expNo }) {
  return db.postData({
    api: '/api/data/call/R02_M07/A10/',
    params: { LOGISTICS_NO: expNo },
  });
}

const Constants = { STORE_NAME };

export { Constants, queryByExpNo };
