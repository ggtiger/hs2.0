export default {
  props: {
  },
  data() {
    return {
      deptParam: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
      },
      provinceParam: {
        loadData: (INPUT, callback) => { this.regSel('100000', INPUT, callback) },
        keyName: 'REGION_CODE',
        titleName: 'REGION_NAME',
      },
      cityParam: {
        loadData: (INPUT, callback) => { this.regSel(this.PROVINCEID || '-1', INPUT, callback) },
        keyName: 'REGION_CODE',
        titleName: 'REGION_NAME',
      },
      countyParam: {
        loadData: (INPUT, callback) => { this.regSel(this.CITYID || '-1', INPUT, callback) },
        keyName: 'REGION_CODE',
        titleName: 'REGION_NAME',
      },
      empParam: {
        loadData: this.empSel,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      custParam: {
        loadData: this.custSel,
        keyName: 'ID',
        titleName: 'CUSTNAME',
      },
    };
  },
  computed: {

  },
  mounted() {
  },
  methods: {
    async empSel(INPUT, callback) {
      if (this.EMPNAME === INPUT) {
        INPUT = '';
      }
      // eslint-disable-next-line no-restricted-syntax
      let ret = await this.$store.dispatch(`${this.storeName}/empSel`, { INPUT });
      callback(ret);
    },
    async deptSel(INPUT, callback) {
      if (this.DEPTNAME === INPUT) {
        INPUT = '';
      }
      // eslint-disable-next-line no-restricted-syntax
      let ret = await this.$store.dispatch(`${this.storeName}/deptSel`, {
        INPUT,
      });
      callback(ret);
    },
    async custSel(INPUT, callback) {
      if (this.CUSTNAME === INPUT) {
        INPUT = '';
      }
      // eslint-disable-next-line no-restricted-syntax
      let ret = await this.$store.dispatch(`${this.storeName}/custSel`, {
        INPUT,
      });
      callback(ret);
    },
    async regSel(PCODE, INPUT, callback) {
      if (this.DEPTNAME === INPUT) {
        INPUT = '';
      }
      // eslint-disable-next-line no-restricted-syntax
      let ret = await this.$store.dispatch(`${this.storeName}/regSel`, {
        INPUT, PCODE
      });
      callback(ret);
    },
  },
  watch: {
  },
};
