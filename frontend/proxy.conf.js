module.exports = {
  '/api': {
    target: process.env.API_URL ?? 'http://127.0.0.1:8080',
    secure: false,
    changeOrigin: true,
  },
};
