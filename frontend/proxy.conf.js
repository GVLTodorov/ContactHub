const target = process.env.API_URL ?? 'http://127.0.0.1:8080';

// The API and its Swagger UI both live on the backend; the dev server forwards them so they also
// open from the frontend's port (http://127.0.0.1:4700/swagger).
module.exports = {
  '/api': { target, secure: false, changeOrigin: true },
  '/swagger': { target, secure: false, changeOrigin: true },
};
