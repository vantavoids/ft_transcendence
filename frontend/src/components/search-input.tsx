import { Search } from 'lucide-react';

type SearchInputProps = {
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  className?: string;
};

export function SearchInput({ value, onChange, placeholder, className = 'mt-4' }: SearchInputProps) {
  return (
    <label className={`flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted ${className}`}>
      <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
      />
    </label>
  );
}
