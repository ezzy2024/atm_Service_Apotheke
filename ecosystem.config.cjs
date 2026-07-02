module.exports = {
  apps: [
    {
      name: "service-apotheke-atm",
      script: "./dist/server.cjs",
      instances: "max",
      exec_mode: "cluster",
      autorestart: true,
      watch: false,
      max_memory_restart: "1G",
      env: {
        NODE_ENV: "production",
        PORT: 4000
      }
    }
  ]
};
