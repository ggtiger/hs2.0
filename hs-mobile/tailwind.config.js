/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{vue,js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {},
  },
  // uni-app 兼容：忽略 preflight（重置样式）避免与 uv-ui 冲突
  corePlugins: {
    preflight: false
  },
  plugins: [],
}
