import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./src/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: "#0B0F19",
        fog: "#F6F7FB",
        accent: "#FF6B35",
      },
    },
  },
  plugins: [],
};

export default config;
