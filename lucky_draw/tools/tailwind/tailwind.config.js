/** Quét các view để chỉ sinh ra những class Tailwind thực sự được dùng */
module.exports = {
  content: {
    relative: true,
    files: ['../../Views/**/*.cshtml']
  },
  theme: { extend: {} },
  plugins: []
};
