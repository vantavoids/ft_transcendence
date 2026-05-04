import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        display: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        category: ['Outfit', 'Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'monospace']
      },
      spacing: {
        120: '30rem',
        140: '35rem'
      },
      colors: {
        'primary-bg': '#191919',
        'secondary-bg': '#09090b',
        panel: '#121213',
        frame: '#1a1a1c',
        stroke: '#242426',
        'grey-link': '#848485',
        muted: '#5b5b5c',
        category: '#545454',
        'input-bg': '#0f0f10',
        'input-placeholder': '#343434',
        aqua: '#78dce8',
        lavender: '#ab9df2',
        pink: '#ff6188',
        orange: '#fc9867',
        yellow: '#ffd866',
        lime: '#a9dc76'
      },
      boxShadow: {
        glow: '0 0 40px rgba(169, 220, 118, 0.28), 0 0 80px rgba(120, 220, 232, 0.2), 0 0 120px rgba(255, 97, 136, 0.16)'
      }
    }
  },
  plugins: []
};

export default config;
