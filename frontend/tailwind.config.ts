import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      spacing: {
        120: '30rem',
        140: '35rem'
      },
      colors: {
        'primary-bg': '#191919',
        'secondary-bg': '#09090b',
        'grey-link': '#848485',
        'input-bg': '#0f0f10',
        'input-placeholder': '#343434'
      }
    }
  },
  plugins: []
};

export default config;
